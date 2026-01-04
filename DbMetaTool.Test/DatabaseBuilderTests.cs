using DbMetaTool.Database;
using DbMetaTool.Scripts;
using DbMetaTool.Services;
using Moq;

namespace DbMetaTool.Test
{
    [TestFixture]
    public class DatabaseBuilderTests
    {
        private Mock<IFirebirdConnection> _mockConnection = null!;
        private Mock<IScriptParser> _mockParser = null!;
        private DatabaseBuilder _builder = null!;

        [SetUp]
        public void Setup()
        {
            _mockConnection = new Mock<IFirebirdConnection>();
            _mockParser = new Mock<IScriptParser>();
            _builder = new DatabaseBuilder(_mockConnection.Object, _mockParser.Object);
        }

        [Test]
        public void Constructor_WithNullFirebirdConnection_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new DatabaseBuilder(null!, _mockParser.Object));

            Assert.That(ex.ParamName, Is.EqualTo("firebirdConnection"));
        }

        [Test]
        public void Constructor_WithNullScriptParser_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new DatabaseBuilder(_mockConnection.Object, null!));

            Assert.That(ex.ParamName, Is.EqualTo("scriptParser"));
        }

        [Test]
        public void Build_WithEmptyDatabaseDirectory_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _builder.Build("", "C:\\scripts"));

            Assert.That(ex.ParamName, Is.EqualTo("databaseDirectory"));
        }

        [Test]
        public void Build_WithEmptyScriptsDirectory_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _builder.Build("C:\\db", ""));

            Assert.That(ex.ParamName, Is.EqualTo("scriptsDirectory"));
        }

        [Test]
        public void Build_WithNonExistentScriptsDirectory_ThrowsDirectoryNotFoundException()
        {
            var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Assert.Throws<DirectoryNotFoundException>(() =>
                _builder.Build("C:\\db", nonExistentDir));
        }

        [Test]
        public void Build_CallsCreateDatabase()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var tempDbDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(new ParsedScripts());

                _mockConnection.Setup(c => c.CreateDatabase(It.IsAny<string>()));

                _builder.Build(tempDbDir, tempScriptsDir);

                _mockConnection.Verify(c => c.CreateDatabase(It.Is<string>(
                    path => path.Contains("database.fdb"))), Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
                if (Directory.Exists(tempDbDir))
                    Directory.Delete(tempDbDir, true);
            }
        }

        [Test]
        public void Build_CallsParseScriptsFromDirectory()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var tempDbDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(new ParsedScripts());

                _mockConnection.Setup(c => c.CreateDatabase(It.IsAny<string>()));

                _builder.Build(tempDbDir, tempScriptsDir);

                _mockParser.Verify(p => p.ParseScriptsFromDirectory(tempScriptsDir), Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
                if (Directory.Exists(tempDbDir))
                    Directory.Delete(tempDbDir, true);
            }
        }

        [Test]
        public void Build_WithNoScripts_DoesNotExecuteScripts()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var tempDbDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(new ParsedScripts());

                _mockConnection.Setup(c => c.CreateDatabase(It.IsAny<string>()));

                _builder.Build(tempDbDir, tempScriptsDir);

                _mockConnection.Verify(c => c.ExecuteNonQuery(
                    It.IsAny<string>(),
                    It.IsAny<string>()), Times.Never);
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
                if (Directory.Exists(tempDbDir))
                    Directory.Delete(tempDbDir, true);
            }
        }

        [Test]
        public void Build_WithDomainScripts_ExecutesScriptsInOrder()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var tempDbDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                var parsedScripts = new ParsedScripts
                {
                    DomainScripts = new List<string>
                    {
                        "CREATE DOMAIN D_ID AS INTEGER;",
                        "CREATE DOMAIN D_NAME AS VARCHAR(100);"
                    }
                };

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(parsedScripts);

                _mockConnection.Setup(c => c.CreateDatabase(It.IsAny<string>()));
                _mockConnection.Setup(c => c.ExecuteNonQuery(It.IsAny<string>(), It.IsAny<string>()));

                _builder.Build(tempDbDir, tempScriptsDir);

                _mockConnection.Verify(c => c.ExecuteNonQuery(
                    It.IsAny<string>(),
                    "CREATE DOMAIN D_ID AS INTEGER;"), Times.Once);

                _mockConnection.Verify(c => c.ExecuteNonQuery(
                    It.IsAny<string>(),
                    "CREATE DOMAIN D_NAME AS VARCHAR(100);"), Times.Once);
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
                if (Directory.Exists(tempDbDir))
                    Directory.Delete(tempDbDir, true);
            }
        }

        [Test]
        public void Build_WithAllScriptTypes_ExecutesInCorrectOrder()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var tempDbDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                var parsedScripts = new ParsedScripts
                {
                    DomainScripts = new List<string> { "CREATE DOMAIN D_ID AS INTEGER;" },
                    TableScripts = new List<string> { "CREATE TABLE USERS (ID D_ID);" },
                    ProcedureScripts = new List<string> { "CREATE PROCEDURE GET_USER AS BEGIN END;" }
                };

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(parsedScripts);

                _mockConnection.Setup(c => c.CreateDatabase(It.IsAny<string>()));

                var executionOrder = new List<string>();
                _mockConnection.Setup(c => c.ExecuteNonQuery(It.IsAny<string>(), It.IsAny<string>()))
                    .Callback<string, string>((_, script) => executionOrder.Add(script));

                _builder.Build(tempDbDir, tempScriptsDir);

                Assert.That(executionOrder.Count, Is.EqualTo(3));
                Assert.That(executionOrder[0], Does.Contain("DOMAIN"));
                Assert.That(executionOrder[1], Does.Contain("TABLE"));
                Assert.That(executionOrder[2], Does.Contain("PROCEDURE"));
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
                if (Directory.Exists(tempDbDir))
                    Directory.Delete(tempDbDir, true);
            }
        }

        [Test]
        public void Build_WithScriptExecutionFailure_ThrowsInvalidOperationException()
        {
            var tempScriptsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var tempDbDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempScriptsDir);

                var parsedScripts = new ParsedScripts
                {
                    DomainScripts = new List<string> { "CREATE DOMAIN D_ID AS INTEGER;" }
                };

                _mockParser.Setup(p => p.ParseScriptsFromDirectory(tempScriptsDir))
                    .Returns(parsedScripts);

                _mockConnection.Setup(c => c.CreateDatabase(It.IsAny<string>()));
                _mockConnection.Setup(c => c.ExecuteNonQuery(It.IsAny<string>(), It.IsAny<string>()))
                    .Throws(new Exception("SQL execution error"));

                Assert.Throws<InvalidOperationException>(() =>
                    _builder.Build(tempDbDir, tempScriptsDir));
            }
            finally
            {
                if (Directory.Exists(tempScriptsDir))
                    Directory.Delete(tempScriptsDir, true);
                if (Directory.Exists(tempDbDir))
                    Directory.Delete(tempDbDir, true);
            }
        }
    }
}
