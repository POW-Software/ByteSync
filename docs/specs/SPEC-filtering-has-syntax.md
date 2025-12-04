# Spécification : Système de Filtrage `has:`

## 1. Contexte et Objectifs

### 1.1 Problématique

Les utilisateurs de ByteSync ne peuvent pas actuellement filtrer les éléments selon leur état d'accessibilité ou d'erreur. Lorsqu'un fichier ou répertoire est inaccessible (permissions, verrouillage, etc.), il ne peut pas être synchronisé, mais l'utilisateur n'a aucun moyen de les identifier rapidement dans l'interface.

### 1.2 Objectifs

1. **Introduire une nouvelle syntaxe `has:`** pour filtrer les éléments selon leur état
2. **Implémenter 3 filtres d'état** :
   - `has:access-issue` - Éléments inaccessibles
   - `has:computation-error` - Éléments avec erreur de calcul de signature
   - `has:sync-error` - Éléments avec erreur de synchronisation
3. **Migrer `actions` vers `has:actions`** (breaking change)

### 1.3 Breaking Change

La syntaxe `actions` est remplacée par `has:actions`. L'ancienne syntaxe ne sera plus supportée.

| Ancienne Syntaxe | Nouvelle Syntaxe |
|------------------|------------------|
| `actions` | `has:actions` |
| `actions > 1` | `has:actions > 1` |
| `actions.copy` | `has:actions.copy` |
| `actions.targeted.delete` | `has:actions.targeted.delete` |

---

## 2. Syntaxe de Filtrage

### 2.1 Filtres Booléens Simples

```
has:access-issue       → Éléments avec problème d'accessibilité
has:computation-error  → Éléments avec erreur de calcul de signature
has:sync-error         → Éléments avec erreur de synchronisation
```

**Négation** : `NOT has:access-issue`

**Combinaisons** :
```
has:access-issue OR has:computation-error
has:access-issue AND is:file
NOT has:sync-error AND has:actions
```

### 2.2 Filtre Actions (migration)

```
has:actions                    → A des actions planifiées (count > 0)
has:actions > 1                → Comparaison numérique
has:actions == 0               → Aucune action
has:actions.copy               → Actions de type copy
has:actions.targeted           → Actions ciblées manuellement
has:actions.rules              → Actions générées par règles
has:actions.targeted.delete    → Combinaison origine + type
```

### 2.3 Grammaire

```
has_expression := "has" ":" has_type [has_modifier]

has_type := "access-issue" 
          | "computation-error" 
          | "sync-error"
          | "actions" [action_path] [comparison]

action_path := ("." action_modifier)*
action_modifier := "targeted" | "rules" | "copy" | "copy-contents" | "copy-dates" | "create" | "delete" | "do-nothing"

comparison := comparison_operator number
comparison_operator := ">" | "<" | ">=" | "<=" | "==" | "!="
```

---

## 3. Architecture Technique

### 3.1 Choix d'Architecture

**Approche retenue : Expression unifiée `HasExpression` + spécialisation pour actions**

Justification :
- Cohérent avec le pattern `FileSystemTypeExpression` qui gère `file` et `dir` via un enum
- Réduit le nombre de classes à créer et maintenir
- `has:actions` conserve sa propre expression (`ActionComparisonExpression`) car elle a une logique spécifique (comparaison numérique, sous-propriétés)

### 3.2 Diagramme des Classes

```
                    FilterExpression (abstract)
                           │
         ┌─────────────────┼─────────────────┐
         │                 │                 │
    HasExpression   ActionComparisonExpression  ... (autres)
         │                 │
         │                 │
    HasExpressionEvaluator  ActionComparisonExpressionEvaluator
```

### 3.3 Enum `HasExpressionType`

```csharp
public enum HasExpressionType
{
    AccessIssue,        // has:access-issue
    ComputationError,   // has:computation-error
    SyncError           // has:sync-error
}
```

---

## 4. Spécifications Détaillées par Fichier

### 4.1 Fichiers à Modifier

#### 4.1.1 `Identifiers.cs`

**Chemin** : `src/ByteSync.Client/Business/Filtering/Parsing/Identifiers.cs`

**Modifications** :

```csharp
// Ajouter les nouvelles constantes :
public const string OPERATOR_HAS = "has";
public const string PROPERTY_ACCESS_ISSUE = "access-issue";
public const string PROPERTY_COMPUTATION_ERROR = "computation-error";
public const string PROPERTY_SYNC_ERROR = "sync-error";
```

**Note** : `OPERATOR_ACTIONS` existe déjà et sera réutilisé pour `has:actions`.

---

#### 4.1.2 `FilterParser.cs`

**Chemin** : `src/ByteSync.Client/Business/Filtering/Parsing/FilterParser.cs`

**Modifications** :

1. **Ajouter le parsing du bloc `has:`** dans la méthode `TryParseFactor()`, avant le bloc `actions` existant (environ ligne 354)

2. **Supprimer le parsing standalone de `actions`** (le bloc actuel lignes 354-408) car il sera intégré dans `has:`

**Logique de parsing `has:`** :

```
Si token == "has":
    Consommer "has"
    Vérifier et consommer ":"
    Lire le type (access-issue | computation-error | sync-error | actions)
    
    Si type == "actions":
        → Déléguer à la logique existante de ActionComparisonExpression
        → Construire le actionPath avec "actions" + les sous-propriétés
    Sinon:
        → Créer HasExpression avec le type approprié
```

**Pseudo-code détaillé** :

```csharp
if (CurrentToken?.Type == FilterTokenType.Identifier &&
    CurrentToken.Token.Equals(Identifiers.OPERATOR_HAS, StringComparison.OrdinalIgnoreCase))
{
    NextToken(); // consume 'has'
    
    if (CurrentToken?.Type != FilterTokenType.Colon)
    {
        return ParseResult.Incomplete($"Expected colon after '{Identifiers.OPERATOR_HAS}'");
    }
    
    NextToken(); // consume ':'
    
    if (CurrentToken?.Type != FilterTokenType.Identifier)
    {
        return ParseResult.Incomplete($"Expected identifier after '{Identifiers.OPERATOR_HAS}:'");
    }
    
    var hasType = CurrentToken.Token.ToLowerInvariant();
    NextToken();
    
    // Cas 1: Filtres booléens simples
    if (hasType == Identifiers.PROPERTY_ACCESS_ISSUE)
    {
        return ParseResult.Success(new HasExpression(HasExpressionType.AccessIssue));
    }
    else if (hasType == Identifiers.PROPERTY_COMPUTATION_ERROR)
    {
        return ParseResult.Success(new HasExpression(HasExpressionType.ComputationError));
    }
    else if (hasType == Identifiers.PROPERTY_SYNC_ERROR)
    {
        return ParseResult.Success(new HasExpression(HasExpressionType.SyncError));
    }
    // Cas 2: has:actions (réutilise la logique existante)
    else if (hasType == Identifiers.OPERATOR_ACTIONS)
    {
        return ParseActionsExpression(); // Méthode extraite de la logique existante
    }
    else
    {
        return ParseResult.Incomplete($"Unknown has type: {hasType}");
    }
}
```

3. **Extraire la logique actions dans une méthode privée** `ParseActionsExpression()` :

```csharp
private ParseResult ParseActionsExpression()
{
    // actionPath commence par "actions"
    var actionPath = Identifiers.OPERATOR_ACTIONS;
    
    // Consommer les sous-propriétés (.targeted, .copy, etc.)
    while (CurrentToken?.Type == FilterTokenType.Dot)
    {
        NextToken();
        if (CurrentToken?.Type != FilterTokenType.Identifier)
        {
            return ParseResult.Incomplete("Expected identifier after dot in action path");
        }
        
        actionPath += "." + CurrentToken.Token.ToLowerInvariant();
        NextToken();
    }
    
    // Si fin d'expression ou opérateur logique → count > 0 implicite
    if (CurrentToken?.Type == FilterTokenType.End || 
        CurrentToken?.Type == FilterTokenType.LogicalOperator ||
        CurrentToken?.Type == FilterTokenType.CloseParenthesis)
    {
        return ParseResult.Success(new ActionComparisonExpression(actionPath, ComparisonOperator.GreaterThan, 0));
    }
    
    // Sinon, parsing de la comparaison numérique
    if (CurrentToken?.Type != FilterTokenType.Operator)
    {
        return ParseResult.Incomplete("Expected operator after action path");
    }
    
    var op = CurrentToken.Token;
    NextToken();
    
    try
    {
        var comparisonOperator = _operatorParser.Parse(op);
        
        if (CurrentToken?.Type != FilterTokenType.Number)
        {
            return ParseResult.Incomplete("Expected numeric value after operator in action comparison");
        }
        
        if (!int.TryParse(CurrentToken.Token, out var value))
        {
            return ParseResult.Incomplete("Invalid numeric value in action comparison");
        }
        
        NextToken();
        
        return ParseResult.Success(new ActionComparisonExpression(actionPath, comparisonOperator, value));
    }
    catch (ArgumentException ex)
    {
        return ParseResult.Incomplete(ex.Message);
    }
}
```

---

#### 4.1.3 `ExpressionEvaluatorFactory.cs`

**Chemin** : `src/ByteSync.Client/Business/Filtering/Evaluators/ExpressionEvaluatorFactory.cs`

**Modifications** :

Ajouter le mapping dans le dictionnaire `_evaluatorTypes` :

```csharp
{ typeof(HasExpression), typeof(HasExpressionEvaluator) }
```

---

### 4.2 Fichiers à Créer

#### 4.2.1 `HasExpressionType.cs`

**Chemin** : `src/ByteSync.Client/Business/Filtering/Expressions/HasExpressionType.cs`

**Contenu complet** :

```csharp
namespace ByteSync.Business.Filtering.Expressions;

public enum HasExpressionType
{
    AccessIssue,
    ComputationError,
    SyncError
}
```

---

#### 4.2.2 `HasExpression.cs`

**Chemin** : `src/ByteSync.Client/Business/Filtering/Expressions/HasExpression.cs`

**Contenu complet** :

```csharp
namespace ByteSync.Business.Filtering.Expressions;

public class HasExpression : FilterExpression
{
    public HasExpressionType ExpressionType { get; }

    public HasExpression(HasExpressionType expressionType)
    {
        ExpressionType = expressionType;
    }
}
```

---

#### 4.2.3 `HasExpressionEvaluator.cs`

**Chemin** : `src/ByteSync.Client/Business/Filtering/Evaluators/HasExpressionEvaluator.cs`

**Contenu complet** :

```csharp
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;

namespace ByteSync.Business.Filtering.Evaluators;

public class HasExpressionEvaluator : ExpressionEvaluator<HasExpression>
{
    public override bool Evaluate(HasExpression expression, ComparisonItem item)
    {
        return expression.ExpressionType switch
        {
            HasExpressionType.AccessIssue => EvaluateAccessIssue(item),
            HasExpressionType.ComputationError => EvaluateComputationError(item),
            HasExpressionType.SyncError => EvaluateSyncError(item),
            _ => throw new ArgumentException($"Unknown HasExpressionType: {expression.ExpressionType}")
        };
    }

    private bool EvaluateAccessIssue(ComparisonItem item)
    {
        return item.ContentIdentities.Any(ci => ci.HasAccessIssue);
    }

    private bool EvaluateComputationError(ComparisonItem item)
    {
        return item.ContentIdentities.Any(ci => ci.HasAnalysisError);
    }

    private bool EvaluateSyncError(ComparisonItem item)
    {
        return item.ItemSynchronizationStatus.IsErrorStatus;
    }
}
```

**Notes d'implémentation** :

- `HasAccessIssue` est une propriété existante sur `ContentIdentity` qui vérifie :
  - `FileSystemDescriptions.Any(fsd => fsd is FileDescription && !fsd.IsAccessible)`
  - `AccessIssueInventoryParts.Count > 0`

- `HasAnalysisError` est une propriété existante sur `ContentIdentity` qui vérifie :
  - `FileSystemDescriptions.Any(fsd => fsd is FileDescription { HasAnalysisError: true })`

- `IsErrorStatus` est une propriété existante sur `ItemSynchronizationStatus`

---

### 4.3 Fichiers de Tests à Créer

#### 4.3.1 `TestFiltering_Has.cs`

**Chemin** : `tests/ByteSync.Client.IntegrationTests/Business/Filtering/TestFiltering_Has.cs`

**Structure** :

```csharp
using ByteSync.Common.Business.Inventories;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using FluentAssertions;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering_Has : BaseTestFiltering
{
    [SetUp]
    public void Setup()
    {
        SetupBase();
    }

    #region has:access-issue

    [Test]
    public void HasAccessIssue_WhenFileIsAccessible_ShouldReturnFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var filterText = "has:access-issue";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void HasAccessIssue_WhenFileIsInaccessible_ShouldReturnTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithInaccessibleFile("A1");
        var filterText = "has:access-issue";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void HasAccessIssue_WhenDirectoryIsInaccessible_ShouldReturnTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithInaccessibleDirectory("A1");
        var filterText = "has:access-issue";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void HasAccessIssue_WithNotOperator_ShouldReturnInverse()
    {
        // Arrange
        var accessibleItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var inaccessibleItem = PrepareComparisonWithInaccessibleFile("A1");
        var filterText = "NOT has:access-issue";

        // Act & Assert
        EvaluateFilterExpression(filterText, accessibleItem).Should().BeTrue();
        EvaluateFilterExpression(filterText, inaccessibleItem).Should().BeFalse();
    }

    [Test]
    public void HasAccessIssue_CaseInsensitive_ShouldWork()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithInaccessibleFile("A1");

        // Act & Assert
        EvaluateFilterExpression("has:access-issue", comparisonItem).Should().BeTrue();
        EvaluateFilterExpression("HAS:Access-Issue", comparisonItem).Should().BeTrue();
        EvaluateFilterExpression("HAS:ACCESS-ISSUE", comparisonItem).Should().BeTrue();
    }

    #endregion

    #region has:computation-error

    [Test]
    public void HasComputationError_WhenNoError_ShouldReturnFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var filterText = "has:computation-error";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void HasComputationError_WhenHasAnalysisError_ShouldReturnTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithAnalysisError("A1");
        var filterText = "has:computation-error";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void HasComputationError_WithNotOperator_ShouldReturnInverse()
    {
        // Arrange
        var normalItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var errorItem = PrepareComparisonWithAnalysisError("A1");
        var filterText = "NOT has:computation-error";

        // Act & Assert
        EvaluateFilterExpression(filterText, normalItem).Should().BeTrue();
        EvaluateFilterExpression(filterText, errorItem).Should().BeFalse();
    }

    #endregion

    #region has:sync-error

    [Test]
    public void HasSyncError_WhenNoError_ShouldReturnFalse()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var filterText = "has:sync-error";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void HasSyncError_WhenHasSyncError_ShouldReturnTrue()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        comparisonItem.ItemSynchronizationStatus.IsErrorStatus = true;
        var filterText = "has:sync-error";

        // Act
        var result = EvaluateFilterExpression(filterText, comparisonItem);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void HasSyncError_WithNotOperator_ShouldReturnInverse()
    {
        // Arrange
        var normalItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        var errorItem = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        errorItem.ItemSynchronizationStatus.IsErrorStatus = true;
        var filterText = "NOT has:sync-error";

        // Act & Assert
        EvaluateFilterExpression(filterText, normalItem).Should().BeTrue();
        EvaluateFilterExpression(filterText, errorItem).Should().BeFalse();
    }

    #endregion

    #region Combinations

    [Test]
    public void HasFilters_WithAndOperator_ShouldCombineCorrectly()
    {
        // Arrange
        var comparisonItem = PrepareComparisonWithInaccessibleFile("A1");
        comparisonItem.ItemSynchronizationStatus.IsErrorStatus = true;

        // Act & Assert
        EvaluateFilterExpression("has:access-issue AND has:sync-error", comparisonItem).Should().BeTrue();
        EvaluateFilterExpression("has:access-issue AND NOT has:sync-error", comparisonItem).Should().BeFalse();
    }

    [Test]
    public void HasFilters_WithOrOperator_ShouldCombineCorrectly()
    {
        // Arrange
        var accessIssueOnly = PrepareComparisonWithInaccessibleFile("A1");
        var syncErrorOnly = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);
        syncErrorOnly.ItemSynchronizationStatus.IsErrorStatus = true;
        var noError = PrepareComparisonWithOneContent("A1", "hash", DateTime.Now, 100);

        var filterText = "has:access-issue OR has:sync-error";

        // Act & Assert
        EvaluateFilterExpression(filterText, accessIssueOnly).Should().BeTrue();
        EvaluateFilterExpression(filterText, syncErrorOnly).Should().BeTrue();
        EvaluateFilterExpression(filterText, noError).Should().BeFalse();
    }

    [Test]
    public void HasFilters_WithIsFileFilter_ShouldCombineCorrectly()
    {
        // Arrange
        var inaccessibleFile = PrepareComparisonWithInaccessibleFile("A1");
        var inaccessibleDir = PrepareComparisonWithInaccessibleDirectory("A1");

        var filterText = "has:access-issue AND is:file";

        // Act & Assert
        EvaluateFilterExpression(filterText, inaccessibleFile).Should().BeTrue();
        EvaluateFilterExpression(filterText, inaccessibleDir).Should().BeFalse();
    }

    #endregion

    #region Parser Error Cases

    [Test]
    public void HasFilter_WithoutColon_ShouldReturnIncomplete()
    {
        // Arrange
        var filterText = "has access-issue";

        // Act
        var parseResult = _filterParser.TryParse(filterText);

        // Assert
        parseResult.IsComplete.Should().BeFalse();
    }

    [Test]
    public void HasFilter_WithUnknownType_ShouldReturnIncomplete()
    {
        // Arrange
        var filterText = "has:unknown-type";

        // Act
        var parseResult = _filterParser.TryParse(filterText);

        // Assert
        parseResult.IsComplete.Should().BeFalse();
        parseResult.ErrorMessage.Should().Contain("Unknown has type");
    }

    [Test]
    public void HasFilter_WithEmptyType_ShouldReturnIncomplete()
    {
        // Arrange
        var filterText = "has:";

        // Act
        var parseResult = _filterParser.TryParse(filterText);

        // Assert
        parseResult.IsComplete.Should().BeFalse();
    }

    #endregion
}
```

---

#### 4.3.2 Modifications de `BaseTestFiltering.cs`

**Chemin** : `tests/ByteSync.Client.IntegrationTests/Business/Filtering/BaseTestFiltering.cs`

**Méthodes helpers à ajouter** :

```csharp
protected ComparisonItem PrepareComparisonWithInaccessibleFile(string dataPartId)
{
    var comparisonItem = CreateBasicComparisonItem(FileSystemTypes.File);

    string letter = dataPartId[0].ToString();

    var inventory = new Inventory { InventoryId = $"Id_{letter}", Code = letter };
    var inventoryPart = new InventoryPart(inventory, $"/testRoot{letter}", FileSystemTypes.Directory);
    inventoryPart.Code = $"{letter}1";

    var fileDescription = new FileDescription
    {
        InventoryPart = inventoryPart,
        RelativePath = "/inaccessible_file.txt",
        IsAccessible = false
    };

    var contentIdentity = new ContentIdentity(null);
    contentIdentity.Add(fileDescription);
    comparisonItem.AddContentIdentity(contentIdentity);

    var dataParts = new Dictionary<string, (InventoryPart, FileDescription)>
    {
        { dataPartId, (inventoryPart, fileDescription) }
    };
    ConfigureDataPartIndex(dataParts);

    return comparisonItem;
}

protected ComparisonItem PrepareComparisonWithInaccessibleDirectory(string dataPartId)
{
    var comparisonItem = CreateBasicComparisonItem(FileSystemTypes.Directory);

    string letter = dataPartId[0].ToString();

    var inventory = new Inventory { InventoryId = $"Id_{letter}", Code = letter };
    var inventoryPart = new InventoryPart(inventory, $"/testRoot{letter}", FileSystemTypes.Directory);
    inventoryPart.Code = $"{letter}1";

    var directoryDescription = new DirectoryDescription
    {
        InventoryPart = inventoryPart,
        RelativePath = "/inaccessible_dir",
        IsAccessible = false
    };

    var contentIdentity = new ContentIdentity(null);
    contentIdentity.Add(directoryDescription);
    contentIdentity.AddAccessIssue(inventoryPart);
    comparisonItem.AddContentIdentity(contentIdentity);

    var dataParts = new Dictionary<string, (InventoryPart, DirectoryDescription)>
    {
        { dataPartId, (inventoryPart, directoryDescription) }
    };
    ConfigureDataPartIndex(dataParts);

    return comparisonItem;
}

protected ComparisonItem PrepareComparisonWithAnalysisError(string dataPartId)
{
    var comparisonItem = CreateBasicComparisonItem(FileSystemTypes.File);

    string letter = dataPartId[0].ToString();

    var inventory = new Inventory { InventoryId = $"Id_{letter}", Code = letter };
    var inventoryPart = new InventoryPart(inventory, $"/testRoot{letter}", FileSystemTypes.Directory);
    inventoryPart.Code = $"{letter}1";

    var fileDescription = new FileDescription
    {
        InventoryPart = inventoryPart,
        RelativePath = "/error_file.txt",
        IsAccessible = true,
        AnalysisErrorDescription = "Simulated analysis error",
        AnalysisErrorType = "TestError"
    };

    var contentIdentity = new ContentIdentity(null);
    contentIdentity.Add(fileDescription);
    comparisonItem.AddContentIdentity(contentIdentity);

    var dataParts = new Dictionary<string, (InventoryPart, FileDescription)>
    {
        { dataPartId, (inventoryPart, fileDescription) }
    };
    ConfigureDataPartIndex(dataParts);

    return comparisonItem;
}
```

---

#### 4.3.3 Modifications de `TestFiltering_Actions.cs`

**Chemin** : `tests/ByteSync.Client.IntegrationTests/Business/Filtering/TestFiltering_Actions.cs`

**Modifications** : Mettre à jour tous les tests pour utiliser la nouvelle syntaxe `has:actions`

| Ancienne valeur | Nouvelle valeur |
|-----------------|-----------------|
| `"actions>0"` | `"has:actions>0"` |
| `"actions"` | `"has:actions"` |
| `"actions==0"` | `"has:actions==0"` |
| `"NOT actions"` | `"NOT has:actions"` |
| `"actions.targeted>0"` | `"has:actions.targeted>0"` |
| `"actions.rules>0"` | `"has:actions.rules>0"` |
| `"actions.delete>0"` | `"has:actions.delete>0"` |
| `"actions.targeted.delete>0"` | `"has:actions.targeted.delete>0"` |
| `"actions.targeted.delete"` | `"has:actions.targeted.delete"` |
| `"actions.rules.copy-contents>0"` | `"has:actions.rules.copy-contents>0"` |
| `"actions.rules.copy-contents"` | `"has:actions.rules.copy-contents"` |
| `"actions.targeted.create==0"` | `"has:actions.targeted.create==0"` |
| `"actions.delete>0 AND actions.create==0"` | `"has:actions.delete>0 AND has:actions.create==0"` |
| `"actions.delete AND NOT actions.create"` | `"has:actions.delete AND NOT has:actions.create"` |

---

#### 4.3.4 Modifications de `TestFiltering_DocumentationExamples.cs`

**Chemin** : `tests/ByteSync.Client.IntegrationTests/Business/Filtering/TestFiltering_DocumentationExamples.cs`

**Modifications** : Mettre à jour les exemples utilisant `actions`

| Ligne | Ancienne valeur | Nouvelle valeur |
|-------|-----------------|-----------------|
| ~63 | `"name == \"*.log\" AND actions.copy"` | `"name == \"*.log\" AND has:actions.copy"` |
| ~76-81 | Tests avec `actions.copy` | `has:actions.copy` |
| ~101 | `"only:A AND actions.copy"` | `"only:A AND has:actions.copy"` |

---

## 5. Ordre d'Implémentation Recommandé

### Phase 1 : Infrastructure de base

1. Créer `HasExpressionType.cs`
2. Créer `HasExpression.cs`
3. Créer `HasExpressionEvaluator.cs`
4. Modifier `Identifiers.cs` (ajouter les nouvelles constantes)
5. Modifier `ExpressionEvaluatorFactory.cs` (ajouter le mapping)

### Phase 2 : Parsing

6. Modifier `FilterParser.cs` :
   - Extraire la logique actions dans `ParseActionsExpression()`
   - Ajouter le bloc de parsing `has:`
   - Supprimer l'ancien bloc standalone `actions`

### Phase 3 : Tests

7. Ajouter les helpers dans `BaseTestFiltering.cs`
8. Créer `TestFiltering_Has.cs`
9. Mettre à jour `TestFiltering_Actions.cs`
10. Mettre à jour `TestFiltering_DocumentationExamples.cs`

### Phase 4 : Validation

11. Exécuter tous les tests de filtrage
12. Vérifier la compilation du projet complet
13. Exécuter les linters

---

## 6. Critères d'Acceptation

### 6.1 Fonctionnels

- [ ] `has:access-issue` filtre correctement les éléments inaccessibles
- [ ] `has:computation-error` filtre correctement les éléments avec erreur d'analyse
- [ ] `has:sync-error` filtre correctement les éléments avec erreur de synchronisation
- [ ] `has:actions` fonctionne comme l'ancien `actions`
- [ ] Toutes les sous-variantes de `has:actions` fonctionnent (`.copy`, `.targeted`, etc.)
- [ ] Les opérateurs logiques (`AND`, `OR`, `NOT`) fonctionnent avec les nouveaux filtres
- [ ] La syntaxe est case-insensitive

### 6.2 Non-Fonctionnels

- [ ] Aucune régression sur les tests existants
- [ ] Couverture de tests ≥ 85% pour les nouvelles classes
- [ ] Pas de warnings de compilation
- [ ] Respect des conventions de code du projet (pas de commentaires, FluentAssertions, etc.)

### 6.3 Breaking Change

- [ ] L'ancienne syntaxe `actions` ne fonctionne plus
- [ ] Tous les tests utilisant `actions` sont migrés vers `has:actions`

---

## 7. Références

### 7.1 Fichiers Sources de Référence

- Pattern Expression/Evaluator : `FileSystemTypeExpression.cs` + `FileSystemTypeExpressionEvaluator.cs`
- Pattern Parsing `is:` : `FilterParser.cs` lignes 264-294
- Modèle `HasAccessIssue` : `ContentIdentity.cs` lignes 38-45
- Modèle `HasAnalysisError` : `ContentIdentity.cs` ligne 35
- Modèle `IsErrorStatus` : `ItemSynchronizationStatus.cs` ligne 16

### 7.2 Tests de Référence

- Structure des tests : `TestFiltering_FileSystemType.cs`
- Tests actions : `TestFiltering_Actions.cs`
- Base de test : `BaseTestFiltering.cs`

### 7.3 Documentation Externe

- Syntaxe de filtrage actuelle : https://www.bytesyncapp.com/documentation/synchronization/filtering-syntax/
