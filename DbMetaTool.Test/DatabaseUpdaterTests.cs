using DbMetaTool.Database;
using DbMetaTool.Models;
using DbMetaTool.Scripts;
using DbMetaTool.Services;
using Moq;

namespace DbMetaTool.Test
{
    [TestFixture]
    public class DatabaseUpdaterTests
    {
        private Mock<IMetadataExtractor> _mockExtractor = null!;
        private Mock<IScriptParser> _mockParser = null!;
        private Mock<IFirebirdConnection> _mockConnection = null!;
        private DatabaseUpdater _updater = null!;

        [SetUp]
        public void Setup()
        {
            _mockExtractor = new Mock<IMetadataExtractor>();
            _mockParser = new Mock<IScriptParser>();
            _mockConnection = new Mock<IFirebirdConnection>();
            _updater = new DatabaseUpdater(_mockExtractor.Object, _mockParser.Object, _mockConnection.Object);
        }

        [Test]
        public void Constructor_WithNullMetadataExtractor_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new DatabaseUpdater(null!, _mockParser.Object, _mockConnection.Object));

            Assert.That(ex.ParamName, Is.EqualTo("metadataExtractor"));
        }

        [Test]
        public void Constructor_WithNullScriptParser_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new DatabaseUpdater(_mockExtractor.Object, null!, _mockConnection.Object));

            Assert.That(ex.ParamName, Is.EqualTo("scriptParser"));
        }

        [Test]
        public void Constructor_WithNullFirebirdConnection_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new DatabaseUpdater(_mockExtractor.Object, _mockParser.Object, null!));

            Assert.That(ex.ParamName, Is.EqualTo("firebirdConnection"));
        }

        [Test]
        public void Update_WithEmptyConnectionString_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _updater.Update("", "C:\\scripts"));

            Assert.That(ex.ParamName, Is.EqualTo("connectionString"));
        }

        [Test]
        public void Update_WithEmptyScriptsDirectory_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _updater.Update("Server=localhost", ""));

            Assert.That(ex.ParamName, Is.EqualTo("scriptsDirectory"));
        }

        [Test]
        public void Update_WithNonExistentScriptsDirectory_ThrowsDirectoryNotFoundException()
        {
            var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Assert.Throws<DirectoryNotFoundException>(() =>
                _updater.Update("Server=localhost", nonExistentDir));
        }

        [Test]
        public void Update_ExtractsExistingMetadata()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                _mockExtractor.Setup(e => e.ExtractDomains(connectionString))
                    .Returns(new List<Domain>());
                _mockExtractor.Setup(e => e.ExtractTables(connectionString))
                    .Returns(new List<Table>());
                _mockExtractor.Setup(e => e.ExtractProcedures(connectionString))
                    .Returns(new List<Procedure>());

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(new ParsedScripts());

                _updater.Update(connectionString, tempScriptsDir);

                _mockExtractor.Verify(e => e.ExtractDomains(connectionString), Times.Once);
                _mockExtractor.Verify(e => e.ExtractTables(connectionString), Times.Once);
                _mockExtractor.Verify(e => e.ExtractProcedures(connectionString), Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
            }
        }

        [Test]
        public void Update_ParsesScriptsFromDirectory()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                _mockExtractor.Setup(e => e.ExtractDomains(connectionString))
                    .Returns(new List<Domain>());
                _mockExtractor.Setup(e => e.ExtractTables(connectionString))
                    .Returns(new List<Table>());
                _mockExtractor.Setup(e => e.ExtractProcedures(connectionString))
                    .Returns(new List<Procedure>());

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(new ParsedScripts());

                _updater.Update(connectionString, tempScriptsDir);

                _mockParser.Verify(p => p.ParseScriptsFromDirectory(tempScriptsDir), Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
            }
        }

        [Test]
        public void Update_WithNoScripts_DoesNotExecuteChanges()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                _mockExtractor.Setup(e => e.ExtractDomains(connectionString))
                    .Returns(new List<Domain>());
                _mockExtractor.Setup(e => e.ExtractTables(connectionString))
                    .Returns(new List<Table>());
                _mockExtractor.Setup(e => e.ExtractProcedures(connectionString))
                    .Returns(new List<Procedure>());

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(new ParsedScripts());

                _updater.Update(connectionString, tempScriptsDir);

                _mockConnection.Verify(c => c.ExecuteNonQuery(
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Never);
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
            }
        }

        [Test]
        public void Update_WithNoChanges_DoesNotExecuteChanges()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                _mockExtractor.Setup(e => e.ExtractDomains(connectionString))
                    .Returns(new List<Domain>());
                _mockExtractor.Setup(e => e.ExtractTables(connectionString))
                    .Returns(new List<Table>());
                _mockExtractor.Setup(e => e.ExtractProcedures(connectionString))
                    .Returns(new List<Procedure>());

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(new ParsedScripts());

                _updater.Update(connectionString, tempScriptsDir);

                _mockConnection.Verify(c => c.ExecuteNonQuery(
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Never);
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
            }
        }

        [Test]
        public void Update_WithNewDomain_CreatesNewDomain()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                _mockExtractor.Setup(e => e.ExtractDomains(connectionString))
                    .Returns(new List<Domain>());
                _mockExtractor.Setup(e => e.ExtractTables(connectionString))
                    .Returns(new List<Table>());
                _mockExtractor.Setup(e => e.ExtractProcedures(connectionString))
                    .Returns(new List<Procedure>());

                var parsedScripts = new ParsedScripts
                {
                    DomainScripts = new List<string> { "CREATE DOMAIN D_NEW AS INTEGER;" }
                };

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(parsedScripts);

                _mockConnection.Setup(c => c.ExecuteNonQuery(It.IsAny<string>(), It.IsAny<string>()));

                _updater.Update(connectionString, tempScriptsDir);

                _mockConnection.Verify(c => c.ExecuteNonQuery(
                    connectionString,
                    "CREATE DOMAIN D_NEW AS INTEGER;"), Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
            }
        }

        [Test]
        public void Update_WithExistingProcedure_DropsAndRecreates()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                var existingProcedures = new List<Procedure>
                {
                    new Procedure { Name = "GET_USER", SourceCode = "AS BEGIN END;" }
                };

                _mockExtractor.Setup(e => e.ExtractDomains(connectionString))
                    .Returns(new List<Domain>());
                _mockExtractor.Setup(e => e.ExtractTables(connectionString))
                    .Returns(new List<Table>());
                _mockExtractor.Setup(e => e.ExtractProcedures(connectionString))
                    .Returns(existingProcedures);

                var parsedScripts = new ParsedScripts
                {
                    ProcedureScripts = new List<string> { "CREATE PROCEDURE GET_USER AS BEGIN SELECT 1; END;" }
                };

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(parsedScripts);

                _mockConnection.Setup(c => c.ExecuteNonQuery(It.IsAny<string>(), It.IsAny<string>()));

                _updater.Update(connectionString, tempScriptsDir);

                _mockConnection.Verify(c => c.ExecuteNonQuery(
                    connectionString,
                    "DROP PROCEDURE GET_USER;"), Times.Once);

                _mockConnection.Verify(c => c.ExecuteNonQuery(
                    connectionString,
                    "CREATE PROCEDURE GET_USER AS BEGIN SELECT 1; END;"), Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
            }
        }

        [Test]
        public void Update_ExecutesChangesInCorrectOrder()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                var existingDomains = new List<Domain>
                {
                    new Domain { Name = "D_OLD", DataType = "INTEGER" }
                };

                var existingTables = new List<Table>
                {
                    new Table
                    {
                        Name = "OLD_TABLE",
                        Columns = new List<Column>
                        {
                            new Column { Name = "ID", DataType = "INTEGER", Position = 0 }
                        }
                    }
                };

                var existingProcedures = new List<Procedure>
                {
                    new Procedure { Name = "OLD_PROC", SourceCode = "AS BEGIN END;" }
                };

                _mockExtractor.Setup(e => e.ExtractDomains(connectionString))
                    .Returns(existingDomains);
                _mockExtractor.Setup(e => e.ExtractTables(connectionString))
                    .Returns(existingTables);
                _mockExtractor.Setup(e => e.ExtractProcedures(connectionString))
                    .Returns(existingProcedures);

                var parsedScripts = new ParsedScripts
                {
                    DomainScripts = new List<string> { "CREATE DOMAIN D_OLD AS VARCHAR(100);" },
                    TableScripts = new List<string> { "CREATE TABLE OLD_TABLE (ID INTEGER, NAME VARCHAR(100));" },
                    ProcedureScripts = new List<string> { "CREATE PROCEDURE OLD_PROC AS BEGIN SELECT 1; END;" }
                };

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(parsedScripts);

                var executionOrder = new List<string>();
                _mockConnection.Setup(c => c.ExecuteNonQuery(It.IsAny<string>(), It.IsAny<string>()))
                    .Callback<string, string>((_, sql) => executionOrder.Add(sql));

                _updater.Update(connectionString, tempScriptsDir);

                Assert.That(executionOrder[0], Does.Contain("DROP PROCEDURE"));
                Assert.That(executionOrder[1], Does.Contain("DROP TABLE"));
                Assert.That(executionOrder[2], Does.Contain("DROP DOMAIN"));
                Assert.That(executionOrder[3], Does.Contain("CREATE DOMAIN"));
                Assert.That(executionOrder[4], Does.Contain("CREATE TABLE"));
                Assert.That(executionOrder[5], Does.Contain("CREATE PROCEDURE"));
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
            }
        }

        [Test]
        public void Update_WithExecutionError_ThrowsInvalidOperationException()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var connectionString = "Server=localhost;Database=test.fdb";

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                _mockExtractor.Setup(e => e.ExtractDomains(connectionString))
                    .Returns(new List<Domain>());
                _mockExtractor.Setup(e => e.ExtractTables(connectionString))
                    .Returns(new List<Table>());
                _mockExtractor.Setup(e => e.ExtractProcedures(connectionString))
                    .Returns(new List<Procedure>());

                var parsedScripts = new ParsedScripts
                {
                    DomainScripts = new List<string> { "CREATE DOMAIN D_NEW AS INTEGER;" }
                };

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(parsedScripts);

                _mockConnection.Setup(c => c.ExecuteNonQuery(It.IsAny<string>(), It.IsAny<string>()))
                    .Throws(new Exception("SQL error"));

                Assert.Throws<InvalidOperationException>(() =>
                    _updater.Update(connectionString, tempScriptsDir));
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
            }
        }
    }
}
