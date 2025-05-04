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
using FluentAssertions;
using Moq;

namespace ByteSync.Client.IntegrationTests.Business.Filtering;

public class TestFiltering : IntegrationTest
{
    private FilterParser _filterParser;
    private ExpressionEvaluatorFactory _evaluatorFactory;

    [SetUp]
    public void Setup()
    {
        RegisterType<OperatorParser, IOperatorParser>();
        RegisterType<FilterTokenizer, IFilterTokenizer>();
        
        /*
        RegisterType<AndExpressionEvaluator, IExpressionEvaluator>();
        // RegisterType<AndExpressionEvaluator, IExpressionEvaluator<AndExpression>>();
        RegisterType<OrExpressionEvaluator, IExpressionEvaluator>();
        // RegisterType<OrExpressionEvaluator, IExpressionEvaluator<OrExpression>>();
        RegisterType<NotExpressionEvaluator, IExpressionEvaluator>();
        // RegisterType<NotExpressionEvaluator, IExpressionEvaluator<NotExpression>>();
        RegisterType<TrueExpressionEvaluator, IExpressionEvaluator>();
        // RegisterType<TrueExpressionEvaluator, IExpressionEvaluator<TrueExpression>>();
        RegisterType<ExistsExpressionEvaluator, IExpressionEvaluator>();
        // RegisterType<ExistsExpressionEvaluator, IExpressionEvaluator<ExistsExpression>>();
        RegisterType<FileSystemTypeExpressionEvaluator, IExpressionEvaluator>();
        // RegisterType<FileSystemTypeExpressionEvaluator, IExpressionEvaluator<FileSystemTypeExpression>>();
        RegisterType<FutureStateExpressionEvaluator, IExpressionEvaluator>();
        // RegisterType<FutureStateExpressionEvaluator, IExpressionEvaluator<FutureStateExpression>>();
        RegisterType<OnlyExpressionEvaluator, IExpressionEvaluator>();
        // RegisterType<OnlyExpressionEvaluator, IExpressionEvaluator<OnlyExpression>>();
        RegisterType<PropertyComparisonExpressionEvaluator, IExpressionEvaluator>();
        // RegisterType<PropertyComparisonExpressionEvaluator, IExpressionEvaluator<PropertyComparisonExpression>>();
        RegisterType<TextSearchExpressionEvaluator, IExpressionEvaluator>();
        // RegisterType<TextSearchExpressionEvaluator, IExpressionEvaluator<TextSearchExpression>>();
        */
        
        // _builder.RegisterType<AndExpressionEvaluator>().As<IExpressionEvaluator>().InstancePerDependency();
        // _builder.RegisterType<OrExpressionEvaluator>().As<IExpressionEvaluator>().InstancePerDependency();
        // _builder.RegisterType<NotExpressionEvaluator>().As<IExpressionEvaluator>().InstancePerDependency();
        // _builder.RegisterType<TrueExpressionEvaluator>().As<IExpressionEvaluator>().InstancePerDependency();
        // _builder.RegisterType<ExistsExpressionEvaluator>().As<IExpressionEvaluator>().InstancePerDependency();
        // _builder.RegisterType<FileSystemTypeExpressionEvaluator>().As<IExpressionEvaluator>().InstancePerDependency();
        // _builder.RegisterType<FutureStateExpressionEvaluator>().As<IExpressionEvaluator>().InstancePerDependency();
        // _builder.RegisterType<OnlyExpressionEvaluator>().As<IExpressionEvaluator>().InstancePerDependency();
        // _builder.RegisterType<PropertyComparisonExpressionEvaluator>().As<IExpressionEvaluator>().InstancePerDependency();
        // _builder.RegisterType<TextSearchExpressionEvaluator>().As<IExpressionEvaluator>().InstancePerDependency();
        //
        
        _builder.RegisterType<AndExpressionEvaluator>().AsSelf().As<IExpressionEvaluator>().SingleInstance();
        _builder.RegisterType<OrExpressionEvaluator>().AsSelf().As<IExpressionEvaluator>().SingleInstance();
        _builder.RegisterType<NotExpressionEvaluator>().AsSelf().As<IExpressionEvaluator>().SingleInstance();
        _builder.RegisterType<TrueExpressionEvaluator>().AsSelf().As<IExpressionEvaluator>().SingleInstance();
        _builder.RegisterType<ExistsExpressionEvaluator>().AsSelf().As<IExpressionEvaluator>().SingleInstance();
        _builder.RegisterType<FileSystemTypeExpressionEvaluator>().AsSelf().As<IExpressionEvaluator>().SingleInstance();
        _builder.RegisterType<FutureStateExpressionEvaluator>().AsSelf().As<IExpressionEvaluator>().SingleInstance();
        _builder.RegisterType<OnlyExpressionEvaluator>().AsSelf().As<IExpressionEvaluator>().SingleInstance();
        _builder.RegisterType<PropertyComparisonExpressionEvaluator>().AsSelf().As<IExpressionEvaluator>().SingleInstance();
        _builder.RegisterType<TextSearchExpressionEvaluator>().AsSelf().As<IExpressionEvaluator>().SingleInstance();
        
        
        
        RegisterType<ExpressionEvaluatorFactory>();
        
        RegisterType<FilterParser>();
        BuildMoqContainer();
        
        _filterParser = Container.Resolve<FilterParser>();
        _evaluatorFactory = Container.Resolve<ExpressionEvaluatorFactory>();
    }

    [Test]
    [TestCase("example", false)]
    [TestCase("ffile1.txt", false)]
    [TestCase("ile", true)]
    [TestCase("file1", true)]
    [TestCase("file1.txt", true)]
    [TestCase("FILE1.TXT", true)]
    public void Parse_SimpleTextSearch_ReturnsCorrectExpression(string filterText, bool expectedResult)
    {
        // Arrange
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "/file1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var mockDataPartIndexer = Container.Resolve<Mock<IDataPartIndexer>>();
        mockDataPartIndexer.Setup(m => m.GetDataPart(It.IsAny<string>()))
            .Returns((DataPart)null);

        // Act
        var expression = _filterParser.Parse(filterText);
        var evaluator = _evaluatorFactory.GetEvaluator(expression);
        bool result = evaluator.Evaluate(expression, comparisonItem);

        result.Should().Be(expectedResult);

        // // Assert
        // Assert.IsInstanceOf<AndExpression>(expression);
        // Assert.IsTrue(((AndExpression)expression).Expressions.Any(e => e is TextSearchExpression));
    }
    
    [Test]
    public void Test_TOOOOOOOOOOOO_RENNNNNNNNNNN()
    {
        // Arrange
        var filterText = "A1.content==B1.content";
        
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "/file1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var lastWriteTime1 = new DateTime(2023, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var inventoryA = new Inventory();
        inventoryA.InventoryId = "Id_A";
        var inventoryPartA1 = new InventoryPart(inventoryA, "/testRootA1", FileSystemTypes.Directory);
        var fileDescriptionA1 = new FileDescription {
            InventoryPart = inventoryPartA1,
            LastWriteTimeUtc = lastWriteTime1,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = "sameHash"
        };
        
        var inventoryB = new Inventory();
        inventoryB.InventoryId = "Id_B";
        var inventoryPartB1 = new InventoryPart(inventoryB, "/testRootB1", FileSystemTypes.Directory);
        var fileDescriptionB1 = new FileDescription {
            InventoryPart = inventoryPartB1,
            LastWriteTimeUtc = lastWriteTime1,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null,
            Sha256 = "sameHash"
        };
        
        var contentIdentityCore = new ContentIdentityCore();
        contentIdentityCore.SignatureHash = "TestHash";
        contentIdentityCore.Size = 21;
        var contentIdentity = new ContentIdentity(contentIdentityCore);
        comparisonItem.AddContentIdentity(contentIdentity);
        contentIdentity.Add(fileDescriptionA1);
        contentIdentity.Add(fileDescriptionB1);

        var mockDataPartIndexer = Container.Resolve<Mock<IDataPartIndexer>>();
        var dataPartA1 = new DataPart("A1", inventoryPartA1);
        mockDataPartIndexer.Setup(m => m.GetDataPart("A1"))
            .Returns(dataPartA1);
        var dataPartA2 = new DataPart("B1", inventoryPartB1);
        mockDataPartIndexer.Setup(m => m.GetDataPart("B1"))
            .Returns(dataPartA2);

        // Act
        var expression = _filterParser.Parse(filterText);
        var evaluator = _evaluatorFactory.GetEvaluator(expression);
        bool result = evaluator.Evaluate(expression, comparisonItem);

        result.Should().Be(true);

        // // Assert
        // Assert.IsInstanceOf<AndExpression>(expression);
        // Assert.IsTrue(((AndExpression)expression).Expressions.Any(e => e is TextSearchExpression));
    }
    
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-01", "==", true)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-01", "<>", false)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-01", "!=", false)]
    [TestCase("hashLeft", "2023-10-01", "hashRight", "2023-10-01", "==", false)]
    [TestCase("hashLeft", "2023-10-01", "hashRight", "2023-10-01", "<>", true)]
    [TestCase("hashLeft", "2023-10-01", "hashRight", "2023-10-01", "!=", true)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-02", "==", false)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-02", "<>", true)]
    [TestCase("sameHash", "2023-10-01", "sameHash", "2023-10-02", "!=", true)]
    public void Test_TOOOOOOOOOOOO_RENNNNNNNNNNN_2(string leftHash, string leftDateTimeStr, string rightHash, string rightDateTimeStr,
        string @operator, bool expectedResult)
    {
        // Arrange
        var filterText = $"A1.contentanddate{@operator}B1.contentanddate";
        
        DateTime leftDateTime = DateTime.Parse(leftDateTimeStr, System.Globalization.CultureInfo.InvariantCulture);
        DateTime rightDateTime = DateTime.Parse(rightDateTimeStr, System.Globalization.CultureInfo.InvariantCulture);
        
        var pathIdentity = new PathIdentity(FileSystemTypes.File, "/file1.txt", "file1.txt", "/file1.txt");
        var comparisonItem = new ComparisonItem(pathIdentity);
        
        var inventoryA = new Inventory();
        inventoryA.InventoryId = "Id_A";
        var inventoryPartA1 = new InventoryPart(inventoryA, "/testRootA1", FileSystemTypes.Directory);
        var fileDescriptionA1 = new FileDescription {
            InventoryPart = inventoryPartA1,
            LastWriteTimeUtc = leftDateTime,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null
        };
        
        var inventoryB = new Inventory();
        inventoryB.InventoryId = "Id_B";
        var inventoryPartB1 = new InventoryPart(inventoryB, "/testRootB1", FileSystemTypes.Directory);
        var fileDescriptionB1 = new FileDescription {
            InventoryPart = inventoryPartB1,
            LastWriteTimeUtc = rightDateTime,
            Size = 100,
            FingerprintMode = FingerprintModes.Sha256,
            SignatureGuid = null
        };
        
        var contentIdentityCoreA1 = new ContentIdentityCore();
        contentIdentityCoreA1.SignatureHash = leftHash;
        contentIdentityCoreA1.Size = 21;
        var contentIdentityA1 = new ContentIdentity(contentIdentityCoreA1);
        comparisonItem.AddContentIdentity(contentIdentityA1);
        contentIdentityA1.Add(fileDescriptionA1);
        if (leftHash == rightHash)
        {
            contentIdentityA1.Add(fileDescriptionB1);
        }
        else
        {
            var contentIdentityCoreB1 = new ContentIdentityCore();
            contentIdentityCoreB1.SignatureHash = rightHash;
            contentIdentityCoreB1.Size = 23;
            var contentIdentityB1 = new ContentIdentity(contentIdentityCoreB1);
            comparisonItem.AddContentIdentity(contentIdentityB1);
            contentIdentityB1.Add(fileDescriptionB1);
        }


        var mockDataPartIndexer = Container.Resolve<Mock<IDataPartIndexer>>();
        var dataPartA1 = new DataPart("A1", inventoryPartA1);
        mockDataPartIndexer.Setup(m => m.GetDataPart("A1"))
            .Returns(dataPartA1);
        var dataPartA2 = new DataPart("B1", inventoryPartB1);
        mockDataPartIndexer.Setup(m => m.GetDataPart("B1"))
            .Returns(dataPartA2);

        // Act
        var expression = _filterParser.Parse(filterText);
        var evaluator = _evaluatorFactory.GetEvaluator(expression);
        bool result = evaluator.Evaluate(expression, comparisonItem);

        result.Should().Be(expectedResult);

        // // Assert
        // Assert.IsInstanceOf<AndExpression>(expression);
        // Assert.IsTrue(((AndExpression)expression).Expressions.Any(e => e is TextSearchExpression));
    }
    
    [TestCase(100, 100, "==", true)]
    [TestCase(100, 100, ">=", true)]
    [TestCase(100, 100, "<=", true)]
    [TestCase(100, 200, "==", false)]
    [TestCase(100, 200, "!=", true)]
    [TestCase(100, 200, "<=", true)]
    [TestCase(100, 200, "<", true)]
    [TestCase(100, 200, ">=", false)]
    [TestCase(100, 200, ">", false)]
    [TestCase(200, 100, ">=", true)]
    [TestCase(200, 100, ">", true)]
    [TestCase(200, 100, "<=", false)]
    [TestCase(200, 100, "<", false)]
    [TestCase(100, 101, "<", true)]
    [TestCase(100, 100, "<", false)]
    [TestCase(101, 100, ">", true)]
    [TestCase(100, 100, ">", false)]
    public void TestSizeComparison(long leftSize, long rightSize, string @operator, bool expectedResult)
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", now, leftSize,
            "B1", "sameHash", now, rightSize);
    
        var filterText = $"A1.size{@operator}B1.size";
    
        // Act
        var expression = _filterParser.Parse(filterText);
        var evaluator = _evaluatorFactory.GetEvaluator(expression);
        bool result = evaluator.Evaluate(expression, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }
    
    // [TestCase(100, 100, "==", true)]
    // [TestCase(100, 100, ">=", true)]
    // [TestCase(100, 100, "<=", true)]
    // [TestCase(100, 200, "==", false)]
    // [TestCase(100, 200, "!=", true)]
    // [TestCase(100, 200, "<=", true)]
    // [TestCase(100, 200, "<", true)]
    // [TestCase(100, 200, ">=", false)]
    // [TestCase(100, 200, ">", false)]
    // [TestCase(200, 100, ">=", true)]
    // [TestCase(200, 100, ">", true)]
    // [TestCase(200, 100, "<=", false)]
    // [TestCase(200, 100, "<", false)]
    // [TestCase(100, 101, "<", true)]
    // [TestCase(100, 100, "<", false)]
    // [TestCase(101, 100, ">", true)]
    
    
    [TestCase(105 * 1024, ">", true)]
    [TestCase(105 * 1024, "<", false)]
    [TestCase(80 * 1024, ">", false)]
    [TestCase(80 * 1024, "<", true)]
    [Test]
    public void TestSizeComparison_2(long leftSize, string @operator, bool expectedResult)
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", now, leftSize,
            "B1", "sameHash", now, 1);
    
        var filterText = $"A1.size{@operator}100kb";
    
        // Act
        var expression = _filterParser.Parse(filterText);
        var evaluator = _evaluatorFactory.GetEvaluator(expression);
        bool result = evaluator.Evaluate(expression, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }
    
    [TestCase("2024-05-01", "2024-05-01", "==", true)]
    [TestCase("2024-05-01", "2024-05-01", ">=", true)]
    [TestCase("2024-05-01", "2024-05-01", "<=", true)]
    [TestCase("2024-05-01", "2024-06-01", "==", false)]
    [TestCase("2024-05-01", "2024-06-01", "!=", true)]
    [TestCase("2024-05-01", "2024-06-01", "<=", true)]
    [TestCase("2024-05-01", "2024-06-01", "<", true)]
    [TestCase("2024-05-01", "2024-06-01", ">=", false)]
    [TestCase("2024-05-01", "2024-06-01", ">", false)]
    [TestCase("2024-06-01", "2024-05-01", ">=", true)]
    [TestCase("2024-06-01", "2024-05-01", ">", true)]
    [TestCase("2024-06-01", "2024-05-01", "<=", false)]
    [TestCase("2024-06-01", "2024-05-01", "<", false)]
    [TestCase("2024-05-01", "2024-06-01", "<", true)]
    [TestCase("2024-05-01", "2024-05-01", "<", false)]
    [TestCase("2024-06-01", "2024-05-01", ">", true)]
    [TestCase("2024-05-01", "2024-05-01", ">", false)]
    public void TestLastWriteTimeComparison(string leftDateTime, string rightDateTime, string @operator, bool expectedResult)
    {
        // Arrange
        var now = DateTime.Now;
        var comparisonItem = PrepareComparisonWithTwoContents(
            "A1", "sameHash", DateTime.Parse(leftDateTime, System.Globalization.CultureInfo.InvariantCulture),
            "B1", "sameHash", DateTime.Parse(rightDateTime, System.Globalization.CultureInfo.InvariantCulture));
    
        var filterText = $"A1.lastwritetime{@operator}B1.lastwritetime";
    
        // Act
        var expression = _filterParser.Parse(filterText);
        var evaluator = _evaluatorFactory.GetEvaluator(expression);
        bool result = evaluator.Evaluate(expression, comparisonItem);
    
        // Assert
        result.Should().Be(expectedResult);
    }

    private ComparisonItem CreateBasicComparisonItem(string filePath = "/file1.txt", string fileName = "file1.txt")
    {
        var pathIdentity = new PathIdentity(FileSystemTypes.File, filePath, fileName, filePath);
        return new ComparisonItem(pathIdentity);
    }

    private (FileDescription, InventoryPart) CreateFileDescription(
        string inventoryId,
        string rootPath,
        DateTime lastWriteTime,
        string hash,
        long size = 100)
    {
        var inventory = new Inventory { InventoryId = inventoryId };
        var inventoryPart = new InventoryPart(inventory, rootPath, FileSystemTypes.Directory);

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

    private void ConfigureDataPartIndex(
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

    private ComparisonItem PrepareComparisonWithTwoContents(
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

    private ComparisonItem PrepareComparisonWithTwoContents(
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

    // [Test]
    // public void Parse_PropertyComparison_ReturnsCorrectExpression()
    // {
    //     // Arrange
    //     var filterText = "file.size > 1024";
    //     var parser = new FilterParser(_dataPartIndexer, _operatorParser);
    //
    //     // Act
    //     var expression = parser.Parse(filterText);
    //
    //     // Assert
    //     Assert.IsInstanceOf<PropertyComparisonExpression>(expression);
    //     var comparisonExpression = (PropertyComparisonExpression)expression;
    //     Assert.AreEqual("size", comparisonExpression.Property);
    // }
    //
    // [Test]
    // public void Evaluate_PropertyComparisonExpression_ReturnsTrue()
    // {
    //     // Arrange
    //     var filterText = "file.size > 1024";
    //     var parser = new FilterParser(_dataPartIndexer, _operatorParser);
    //     var expression = parser.Parse(filterText);
    //
    //     var comparisonItem = new ComparisonItem(new PathIdentity("testFile", FileSystemTypes.File))
    //     {
    //         ContentRepartition = { Size = 2048 }
    //     };
    //
    //     // Act
    //     var result = expression.Evaluate(comparisonItem);
    //
    //     // Assert
    //     Assert.IsTrue(result);
    // }
    //
    // [Test]
    // public void Parse_InvalidExpression_ThrowsException()
    // {
    //     // Arrange
    //     var filterText = "file.size >";
    //     var parser = new FilterParser(_dataPartIndexer, _operatorParser);
    //
    //     // Act & Assert
    //     Assert.Throws<InvalidOperationException>(() => parser.Parse(filterText));
    // }
}
