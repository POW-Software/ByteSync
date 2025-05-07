using Autofac;
using ByteSync.Business;
using ByteSync.Business.Comparisons;
using ByteSync.Business.Filtering.Evaluators;
using ByteSync.Business.Filtering.Expressions;
using ByteSync.Business.Filtering.Parsing;
using ByteSync.Business.Inventories;
using ByteSync.Common.Business.Inventories;
using ByteSync.Interfaces.Services.Filtering;
using ByteSync.Interfaces.Services.Sessions;
using ByteSync.Models.Comparisons.Result;
using ByteSync.Models.FileSystems;
using ByteSync.Models.Inventories;
using ByteSync.TestsCommon;
using Moq;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public abstract class BaseTestFiltering : IntegrationTest
{
    protected FilterParser _filterParser = null!;
    protected ExpressionEvaluatorFactory _evaluatorFactory = null!;

    // [SetUp]
    protected void SetupBase()
    {
        RegisterType<OperatorParser, IOperatorParser>();
        RegisterType<FilterTokenizer, IFilterTokenizer>();
        RegisterType<PropertyValueExtractor, IPropertyValueExtractor>();
        RegisterType<PropertyComparer, IPropertyComparer>();
        
        RegisterType<AndExpressionEvaluator, IExpressionEvaluator>();
        RegisterType<OrExpressionEvaluator, IExpressionEvaluator>();
        RegisterType<NotExpressionEvaluator, IExpressionEvaluator>();
        RegisterType<TrueExpressionEvaluator, IExpressionEvaluator>();
        RegisterType<ExistsExpressionEvaluator, IExpressionEvaluator>();
        RegisterType<FileSystemTypeExpressionEvaluator, IExpressionEvaluator>();
        RegisterType<FutureStateExpressionEvaluator, IExpressionEvaluator>();
        RegisterType<OnlyExpressionEvaluator, IExpressionEvaluator>();
        RegisterType<PropertyComparisonExpressionEvaluator, IExpressionEvaluator>();
        RegisterType<TextSearchExpressionEvaluator, IExpressionEvaluator>();
        
        RegisterType<ExpressionEvaluatorFactory, IExpressionEvaluatorFactory>();
        RegisterType<FilterParser>();
        BuildMoqContainer();
        
        _filterParser = Container.Resolve<FilterParser>();
        _evaluatorFactory = Container.Resolve<ExpressionEvaluatorFactory>();
    }

    protected ComparisonItem CreateBasicComparisonItem(string filePath = "/file1.txt", string fileName = "file1.txt")
    {
        var pathIdentity = new PathIdentity(FileSystemTypes.File, filePath, fileName, filePath);
        return new ComparisonItem(pathIdentity);
    }

    protected (FileDescription, InventoryPart) CreateFileDescription(
        string inventoryId,
        string rootPath,
        DateTime lastWriteTime,
        string hash,
        long size = 100)
    {
        string letter = inventoryId.Replace("Id_", "");
        var inventory = new Inventory { InventoryId = inventoryId, Letter = letter };
        
        string code = $"{letter}1";
        var inventoryPart = new InventoryPart(inventory, rootPath, FileSystemTypes.Directory);
        inventoryPart.Code = code;

        var fileDescription = new FileDescription
        {
            InventoryPart = inventoryPart,
            LastWriteTimeUtc = lastWriteTime,
            Size = size,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = hash
        };

        return (fileDescription, inventoryPart);
    }

    protected void ConfigureDataPartIndex(
        Dictionary<string, (InventoryPart, FileDescription)> dataParts)
    {
        var mockDataPartIndexer = Container.Resolve<Mock<IDataPartIndexer>>();

        foreach (var pair in dataParts)
        {
            var dataPart = new DataPart(pair.Key, pair.Value.Item1);
            mockDataPartIndexer.Setup(m => m.GetDataPart(pair.Key))
                .Returns(dataPart);
        }
    }

    protected ComparisonItem PrepareComparisonWithTwoContents(
        string leftDataPartId,
        string leftHash,
        DateTime leftDateTime,
        string rightDataPartId,
        string rightHash,
        DateTime rightDateTime
        )
    {
        return PrepareComparisonWithTwoContents(
            leftDataPartId,
            leftHash,
            leftDateTime,
            100,
            rightDataPartId,
            rightHash,
            rightDateTime,
            100);
    }

    protected ComparisonItem PrepareComparisonWithTwoContents(
        string leftDataPartId,
        string leftHash,
        DateTime leftDateTime,
        long leftSize,
        string rightDataPartId,
        string rightHash,
        DateTime rightDateTime,
        long rightSize)
    {
        var comparisonItem = CreateBasicComparisonItem();

        var (fileDescA, inventoryPartA) = CreateFileDescription(
            "Id_A",
            "/testRootA",
            leftDateTime,
            leftHash,
            leftSize);

        var (fileDescB, inventoryPartB) = CreateFileDescription(
            "Id_B",
            "/testRootB",
            rightDateTime,
            rightHash,
            rightSize);

        // Créer et ajouter les identités de contenu
        var contentIdCoreA = new ContentIdentityCore
        {
            SignatureHash = leftHash,
            Size = 21
        };
        var contentIdA = new ContentIdentity(contentIdCoreA);
        comparisonItem.AddContentIdentity(contentIdA);
        contentIdA.Add(fileDescA);

        if (leftHash == rightHash)
        {
            contentIdA.Add(fileDescB);
        }
        else
        {
            var contentIdCoreB = new ContentIdentityCore
            {
                SignatureHash = rightHash,
                Size = 23
            };
            var contentIdB = new ContentIdentity(contentIdCoreB);
            comparisonItem.AddContentIdentity(contentIdB);
            contentIdB.Add(fileDescB);
        }

        // Configurer l'indexeur de DataPart
        var dataParts = new Dictionary<string, (InventoryPart, FileDescription)>
        {
            { leftDataPartId, (inventoryPartA, fileDescA) },
            { rightDataPartId, (inventoryPartB, fileDescB) }
        };

        ConfigureDataPartIndex(dataParts);

        return comparisonItem;
    }

    protected ComparisonItem PrepareComparisonWithOneContent(
        string dataPartId,
        string leftHash,
        DateTime leftDateTime,
        long leftSize)
    {
        var comparisonItem = CreateBasicComparisonItem();

        string letter = dataPartId[0].ToString();

        var (fileDesc, inventoryPart) = CreateFileDescription(
            $"Id_{letter}",
            $"/testRoot{letter}",
            leftDateTime,
            leftHash,
            leftSize);
        
        var contentIdCore = new ContentIdentityCore
        {
            SignatureHash = leftHash,
            Size = 21
        };
        var contentId = new ContentIdentity(contentIdCore);
        comparisonItem.AddContentIdentity(contentId);
        contentId.Add(fileDesc);
        
        var dataParts = new Dictionary<string, (InventoryPart, FileDescription)>
        {
            { dataPartId, (inventoryPart, fileDesc) }
        };

        ConfigureDataPartIndex(dataParts);

        return comparisonItem;
    }
    
    protected bool EvaluateFilterExpression(string filterText, ComparisonItem item)
    {
        var parseResult = _filterParser.TryParse(filterText);
        if (!parseResult.IsComplete)
        {
            // Test will clearly show what's happening rather than just failing
            throw new InvalidOperationException($"Parse error: {parseResult.ErrorMessage}");
        }
        
        var expression = parseResult.Expression!;
        var evaluator = _evaluatorFactory.GetEvaluator(expression);
        return evaluator.Evaluate(expression, item);
    }
}