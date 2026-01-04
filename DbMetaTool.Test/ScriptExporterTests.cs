using DbMetaTool.Database;
using DbMetaTool.Models;
using DbMetaTool.Scripts;
using DbMetaTool.Services;
using Moq;

namespace DbMetaTool.Test
{
    [TestFixture]
    public class ScriptExporterTests
    {
        private Mock<IMetadataExtractor> _mockMetadataExtractor = null!;
        private Mock<IScriptGenerator> _mockScriptGenerator = null!;
        private ScriptExporter _exporter = null!;

        [SetUp]
        public void Setup()
        {
            _mockMetadataExtractor = new Mock<IMetadataExtractor>();
            _mockScriptGenerator = new Mock<IScriptGenerator>();
            _exporter = new ScriptExporter(_mockMetadataExtractor.Object, _mockScriptGenerator.Object);
        }

        [Test]
        public void Constructor_WithNullMetadataExtractor_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new ScriptExporter(null!, _mockScriptGenerator.Object));

            Assert.That(ex.ParamName, Is.EqualTo("metadataExtractor"));
        }

        [Test]
        public void Constructor_WithNullScriptGenerator_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new ScriptExporter(_mockMetadataExtractor.Object, null!));

            Assert.That(ex.ParamName, Is.EqualTo("scriptGenerator"));
        }

        [Test]
        public void Export_WithEmptyConnectionString_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _exporter.Export("", "C:\\output"));

            Assert.That(ex.ParamName, Is.EqualTo("connectionString"));
        }

        [Test]
        public void Export_WithEmptyOutputDirectory_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _exporter.Export("Server=localhost;Database=test.fdb", ""));

            Assert.That(ex.ParamName, Is.EqualTo("outputDirectory"));
        }

        [Test]
        public void Export_WithValidParameters_ExtractsMetadata()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                _mockMetadataExtractor.Setup(m => m.ExtractDomains(connectionString))
                    .Returns(new List<Domain>());
                _mockMetadataExtractor.Setup(m => m.ExtractTables(connectionString))
                    .Returns(new List<Table>());
                _mockMetadataExtractor.Setup(m => m.ExtractProcedures(connectionString))
                    .Returns(new List<Procedure>());

                _mockScriptGenerator.Setup(s => s.SaveToFiles(
                    It.IsAny<string>(),
                    It.IsAny<List<Domain>>(),
                    It.IsAny<List<Table>>(),
                    It.IsAny<List<Procedure>>()));

                _exporter.Export(connectionString, tempDir);

                _mockMetadataExtractor.Verify(m => m.ExtractDomains(connectionString), Times.Once);
                _mockMetadataExtractor.Verify(m => m.ExtractTables(connectionString), Times.Once);
                _mockMetadataExtractor.Verify(m => m.ExtractProcedures(connectionString), Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void Export_WithValidParameters_CallsSaveToFiles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                var domains = new List<Domain>
                {
                    new Domain { Name = "D_ID", DataType = "INTEGER" }
                };

                var tables = new List<Table>
                {
                    new Table
                    {
                        Name = "USERS",
                        Columns = new List<Column>
                        {
                            new Column { Name = "ID", DataType = "INTEGER", Position = 0 }
                        }
                    }
                };

                var procedures = new List<Procedure>
                {
                    new Procedure { Name = "GET_USER", SourceCode = "AS BEGIN END;" }
                };

                _mockMetadataExtractor.Setup(m => m.ExtractDomains(connectionString))
                    .Returns(domains);
                _mockMetadataExtractor.Setup(m => m.ExtractTables(connectionString))
                    .Returns(tables);
                _mockMetadataExtractor.Setup(m => m.ExtractProcedures(connectionString))
                    .Returns(procedures);

                _mockScriptGenerator.Setup(s => s.SaveToFiles(
                    It.IsAny<string>(),
                    It.IsAny<List<Domain>>(),
                    It.IsAny<List<Table>>(),
                    It.IsAny<List<Procedure>>()));

                _exporter.Export(connectionString, tempDir);

                _mockScriptGenerator.Verify(s => s.SaveToFiles(
                    It.IsAny<string>(),
                    domains,
                    tables,
                    procedures), Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void Export_WithNoMetadata_DoesNotCallSaveToFiles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                _mockMetadataExtractor.Setup(m => m.ExtractDomains(connectionString))
                    .Returns(new List<Domain>());
                _mockMetadataExtractor.Setup(m => m.ExtractTables(connectionString))
                    .Returns(new List<Table>());
                _mockMetadataExtractor.Setup(m => m.ExtractProcedures(connectionString))
                    .Returns(new List<Procedure>());

                _exporter.Export(connectionString, tempDir);

                _mockScriptGenerator.Verify(s => s.SaveToFiles(
                    It.IsAny<string>(),
                    It.IsAny<List<Domain>>(),
                    It.IsAny<List<Table>>(),
                    It.IsAny<List<Procedure>>()), Times.Never);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void Export_CreatesOutputDirectoryIfNotExists()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                _mockMetadataExtractor.Setup(m => m.ExtractDomains(connectionString))
                    .Returns(new List<Domain> { new Domain { Name = "D_ID", DataType = "INTEGER" } });
                _mockMetadataExtractor.Setup(m => m.ExtractTables(connectionString))
                    .Returns(new List<Table>());
                _mockMetadataExtractor.Setup(m => m.ExtractProcedures(connectionString))
                    .Returns(new List<Procedure>());

                _mockScriptGenerator.Setup(s => s.SaveToFiles(
                    It.IsAny<string>(),
                    It.IsAny<List<Domain>>(),
                    It.IsAny<List<Table>>(),
                    It.IsAny<List<Procedure>>()));

                _exporter.Export(connectionString, tempDir);

                Assert.That(Directory.Exists(tempDir), Is.True);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
