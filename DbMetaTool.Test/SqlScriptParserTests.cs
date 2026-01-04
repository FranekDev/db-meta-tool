using DbMetaTool.Scripts;

namespace DbMetaTool.Test
{
    [TestFixture]
    public class SqlScriptParserTests
    {
        private SqlScriptParser _parser = null!;

        [SetUp]
        public void Setup()
        {
            _parser = new SqlScriptParser();
        }

        [Test]
        public void DetectScriptType_WithNullOrEmptyContent_ReturnsUnknown()
        {
            Assert.That(_parser.DetectScriptType(null!), Is.EqualTo(ScriptType.Unknown));
            Assert.That(_parser.DetectScriptType(""), Is.EqualTo(ScriptType.Unknown));
            Assert.That(_parser.DetectScriptType("   "), Is.EqualTo(ScriptType.Unknown));
        }

        [Test]
        public void DetectScriptType_WithCreateDomain_ReturnsDomain()
        {
            var script = "CREATE DOMAIN D_ID AS INTEGER NOT NULL;";
            Assert.That(_parser.DetectScriptType(script), Is.EqualTo(ScriptType.Domain));
        }

        [Test]
        public void DetectScriptType_WithCreateDomainCaseInsensitive_ReturnsDomain()
        {
            var script = "create domain d_name as varchar(100);";
            Assert.That(_parser.DetectScriptType(script), Is.EqualTo(ScriptType.Domain));
        }

        [Test]
        public void DetectScriptType_WithCreateTable_ReturnsTable()
        {
            var script = @"CREATE TABLE USERS (
                ID INTEGER NOT NULL,
                NAME VARCHAR(100)
            );";
            Assert.That(_parser.DetectScriptType(script), Is.EqualTo(ScriptType.Table));
        }

        [Test]
        public void DetectScriptType_WithCreateTableCaseInsensitive_ReturnsTable()
        {
            var script = "create table products (id integer);";
            Assert.That(_parser.DetectScriptType(script), Is.EqualTo(ScriptType.Table));
        }

        [Test]
        public void DetectScriptType_WithCreateProcedure_ReturnsProcedure()
        {
            var script = @"CREATE PROCEDURE GET_USER
            AS
            BEGIN
                SELECT * FROM USERS;
            END;";
            Assert.That(_parser.DetectScriptType(script), Is.EqualTo(ScriptType.Procedure));
        }

        [Test]
        public void DetectScriptType_WithCreateProcedureCaseInsensitive_ReturnsProcedure()
        {
            var script = "create procedure test_proc as begin end;";
            Assert.That(_parser.DetectScriptType(script), Is.EqualTo(ScriptType.Procedure));
        }

        [Test]
        public void DetectScriptType_WithComments_IgnoresComments()
        {
            var script = @"-- This is a comment
            /* Multi-line
               comment */
            CREATE DOMAIN D_ID AS INTEGER;";
            Assert.That(_parser.DetectScriptType(script), Is.EqualTo(ScriptType.Domain));
        }

        [Test]
        public void DetectScriptType_WithOnlyComments_ReturnsUnknown()
        {
            var script = @"-- Just a comment
            /* Another comment */";
            Assert.That(_parser.DetectScriptType(script), Is.EqualTo(ScriptType.Unknown));
        }

        [Test]
        public void DetectScriptType_WithUnknownScript_ReturnsUnknown()
        {
            var script = "SELECT * FROM USERS;";
            Assert.That(_parser.DetectScriptType(script), Is.EqualTo(ScriptType.Unknown));
        }

        [Test]
        public void ParseScriptsFromDirectory_WithEmptyDirectory_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _parser.ParseScriptsFromDirectory(""));

            Assert.That(ex.ParamName, Is.EqualTo("scriptsDirectory"));
        }

        [Test]
        public void ParseScriptsFromDirectory_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
        {
            var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Assert.Throws<DirectoryNotFoundException>(() =>
                _parser.ParseScriptsFromDirectory(nonExistentDir));
        }

        [Test]
        public void ParseScriptsFromDirectory_WithEmptyDirectory_ReturnsEmptyResult()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempDir);

                var result = _parser.ParseScriptsFromDirectory(tempDir);

                Assert.That(result.DomainScripts.Count, Is.EqualTo(0));
                Assert.That(result.TableScripts.Count, Is.EqualTo(0));
                Assert.That(result.ProcedureScripts.Count, Is.EqualTo(0));
                Assert.That(result.TotalCount, Is.EqualTo(0));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void ParseScriptsFromDirectory_WithStructuredDirectory_LoadsScriptsCorrectly()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                var domainsDir = Path.Combine(tempDir, "domains");
                var tablesDir = Path.Combine(tempDir, "tables");
                var proceduresDir = Path.Combine(tempDir, "procedures");

                Directory.CreateDirectory(domainsDir);
                Directory.CreateDirectory(tablesDir);
                Directory.CreateDirectory(proceduresDir);

                File.WriteAllText(Path.Combine(domainsDir, "D_ID.sql"), "CREATE DOMAIN D_ID AS INTEGER;");
                File.WriteAllText(Path.Combine(domainsDir, "D_NAME.sql"), "CREATE DOMAIN D_NAME AS VARCHAR(100);");
                File.WriteAllText(Path.Combine(tablesDir, "USERS.sql"), "CREATE TABLE USERS (ID INTEGER);");
                File.WriteAllText(Path.Combine(proceduresDir, "GET_USER.sql"), "CREATE PROCEDURE GET_USER AS BEGIN END;");

                var result = _parser.ParseScriptsFromDirectory(tempDir);

                Assert.That(result.DomainScripts.Count, Is.EqualTo(2));
                Assert.That(result.TableScripts.Count, Is.EqualTo(1));
                Assert.That(result.ProcedureScripts.Count, Is.EqualTo(1));
                Assert.That(result.TotalCount, Is.EqualTo(4));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void ParseScriptsFromDirectory_WithFlatDirectory_DetectsScriptTypes()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(tempDir);

                File.WriteAllText(Path.Combine(tempDir, "domain1.sql"), "CREATE DOMAIN D_ID AS INTEGER;");
                File.WriteAllText(Path.Combine(tempDir, "table1.sql"), "CREATE TABLE USERS (ID INTEGER);");
                File.WriteAllText(Path.Combine(tempDir, "proc1.sql"), "CREATE PROCEDURE GET_USER AS BEGIN END;");

                var result = _parser.ParseScriptsFromDirectory(tempDir);

                Assert.That(result.DomainScripts.Count, Is.EqualTo(1));
                Assert.That(result.TableScripts.Count, Is.EqualTo(1));
                Assert.That(result.ProcedureScripts.Count, Is.EqualTo(1));
                Assert.That(result.TotalCount, Is.EqualTo(3));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void ParseScriptsFromDirectory_IgnoresEmptyFiles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                var domainsDir = Path.Combine(tempDir, "domains");
                Directory.CreateDirectory(domainsDir);

                File.WriteAllText(Path.Combine(domainsDir, "D_ID.sql"), "CREATE DOMAIN D_ID AS INTEGER;");
                File.WriteAllText(Path.Combine(domainsDir, "empty.sql"), "");
                File.WriteAllText(Path.Combine(domainsDir, "whitespace.sql"), "   ");

                var result = _parser.ParseScriptsFromDirectory(tempDir);

                Assert.That(result.DomainScripts.Count, Is.EqualTo(1));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void ParseScriptsFromDirectory_WithInvalidFile_ContinuesProcessing()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                var domainsDir = Path.Combine(tempDir, "domains");
                Directory.CreateDirectory(domainsDir);

                File.WriteAllText(Path.Combine(domainsDir, "valid.sql"), "CREATE DOMAIN D_ID AS INTEGER;");
                File.WriteAllText(Path.Combine(domainsDir, "invalid.sql"), "INVALID SQL CONTENT");

                var result = _parser.ParseScriptsFromDirectory(tempDir);

                Assert.That(result.DomainScripts.Count, Is.GreaterThanOrEqualTo(1));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void ParseScriptsFromDirectory_OnlyLoadsNonSystemScripts()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                var domainsDir = Path.Combine(tempDir, "domains");
                Directory.CreateDirectory(domainsDir);

                File.WriteAllText(Path.Combine(domainsDir, "D_CUSTOM.sql"), "CREATE DOMAIN D_CUSTOM AS INTEGER;");
                File.WriteAllText(Path.Combine(domainsDir, "D_USER.sql"), "CREATE DOMAIN D_USER AS VARCHAR(50);");

                var result = _parser.ParseScriptsFromDirectory(tempDir);

                Assert.That(result.DomainScripts.Count, Is.EqualTo(2));
                Assert.That(result.DomainScripts.Any(s => s.Contains("D_CUSTOM")), Is.True);
                Assert.That(result.DomainScripts.Any(s => s.Contains("D_USER")), Is.True);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
