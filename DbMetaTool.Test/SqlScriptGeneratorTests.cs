using DbMetaTool.Models;
using DbMetaTool.Scripts;

namespace DbMetaTool.Test
{
    [TestFixture]
    public class SqlScriptGeneratorTests
    {
        private SqlScriptGenerator _generator = null!;

        [SetUp]
        public void Setup()
        {
            _generator = new SqlScriptGenerator();
        }

        [Test]
        public void GenerateDomainScript_WithNullDomain_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _generator.GenerateDomainScript(null!));
        }

        [Test]
        public void GenerateDomainScript_WithBasicDomain_GeneratesCorrectScript()
        {
            var domain = new Domain
            {
                Name = "D_NAME",
                DataType = "VARCHAR(100)",
                IsNullable = true
            };

            var result = _generator.GenerateDomainScript(domain);

            Assert.That(result, Is.EqualTo("CREATE DOMAIN D_NAME AS VARCHAR(100);"));
        }

        [Test]
        public void GenerateDomainScript_WithNotNullDomain_GeneratesCorrectScript()
        {
            var domain = new Domain
            {
                Name = "D_ID",
                DataType = "INTEGER",
                IsNullable = false
            };

            var result = _generator.GenerateDomainScript(domain);

            Assert.That(result, Is.EqualTo("CREATE DOMAIN D_ID AS INTEGER NOT NULL;"));
        }

        [Test]
        public void GenerateDomainScript_WithDefaultValue_GeneratesCorrectScript()
        {
            var domain = new Domain
            {
                Name = "D_STATUS",
                DataType = "CHAR(1)",
                IsNullable = false,
                DefaultValue = "DEFAULT 'A'"
            };

            var result = _generator.GenerateDomainScript(domain);

            Assert.That(result, Is.EqualTo("CREATE DOMAIN D_STATUS AS CHAR(1) NOT NULL DEFAULT 'A';"));
        }

        [Test]
        public void GenerateDomainScript_WithAllOptions_GeneratesCorrectScript()
        {
            var domain = new Domain
            {
                Name = "D_PRICE",
                DataType = "DECIMAL(10,2)",
                IsNullable = false,
                DefaultValue = "DEFAULT 0.00",
            };

            var result = _generator.GenerateDomainScript(domain);

            Assert.That(result, Is.EqualTo("CREATE DOMAIN D_PRICE AS DECIMAL(10,2) NOT NULL DEFAULT 0.00;"));
        }

        [Test]
        public void GenerateTableScript_WithNullTable_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _generator.GenerateTableScript(null!));
        }

        [Test]
        public void GenerateTableScript_WithNoColumns_ThrowsInvalidOperationException()
        {
            var table = new Table
            {
                Name = "EMPTY_TABLE",
                Columns = new List<Column>()
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _generator.GenerateTableScript(table));
            Assert.That(ex.Message, Does.Contain("nie ma kolumn"));
        }

        [Test]
        public void GenerateTableScript_WithSingleColumn_GeneratesCorrectScript()
        {
            var table = new Table
            {
                Name = "SIMPLE_TABLE",
                Columns = new List<Column>
                {
                    new Column
                    {
                        Name = "ID",
                        DataType = "INTEGER",
                        IsNullable = false,
                        Position = 0
                    }
                }
            };

            var result = _generator.GenerateTableScript(table);

            var expected = $"CREATE TABLE SIMPLE_TABLE ({Environment.NewLine}    ID INTEGER NOT NULL{Environment.NewLine});";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GenerateTableScript_WithMultipleColumns_GeneratesCorrectScript()
        {
            var table = new Table
            {
                Name = "USERS",
                Columns = new List<Column>
                {
                    new Column { Name = "ID", DataType = "INTEGER", IsNullable = false, Position = 0 },
                    new Column { Name = "NAME", DataType = "VARCHAR(100)", IsNullable = false, Position = 1 },
                    new Column { Name = "EMAIL", DataType = "VARCHAR(255)", IsNullable = true, Position = 2 }
                }
            };

            var result = _generator.GenerateTableScript(table);

            var expected = $"CREATE TABLE USERS ({Environment.NewLine}    ID INTEGER NOT NULL,{Environment.NewLine}    NAME VARCHAR(100) NOT NULL,{Environment.NewLine}    EMAIL VARCHAR(255){Environment.NewLine});";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GenerateTableScript_WithDomainColumn_GeneratesCorrectScript()
        {
            var table = new Table
            {
                Name = "PRODUCTS",
                Columns = new List<Column>
                {
                    new Column
                    {
                        Name = "ID",
                        DomainName = "D_ID",
                        DataType = "INTEGER",
                        IsNullable = false,
                        Position = 0
                    },
                    new Column
                    {
                        Name = "PRICE",
                        DomainName = "D_PRICE",
                        DataType = "DECIMAL(10,2)",
                        IsNullable = false,
                        Position = 1
                    }
                }
            };

            var result = _generator.GenerateTableScript(table);

            var expected = $"CREATE TABLE PRODUCTS ({Environment.NewLine}    ID D_ID NOT NULL,{Environment.NewLine}    PRICE D_PRICE NOT NULL{Environment.NewLine});";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GenerateTableScript_WithDefaultValue_GeneratesCorrectScript()
        {
            var table = new Table
            {
                Name = "SETTINGS",
                Columns = new List<Column>
                {
                    new Column
                    {
                        Name = "ID",
                        DataType = "INTEGER",
                        IsNullable = false,
                        Position = 0
                    },
                    new Column
                    {
                        Name = "STATUS",
                        DataType = "CHAR(1)",
                        IsNullable = false,
                        DefaultValue = "DEFAULT 'A'",
                        Position = 1
                    }
                }
            };

            var result = _generator.GenerateTableScript(table);

            var expected = $"CREATE TABLE SETTINGS ({Environment.NewLine}    ID INTEGER NOT NULL,{Environment.NewLine}    STATUS CHAR(1) NOT NULL DEFAULT 'A'{Environment.NewLine});";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GenerateProcedureScript_WithNullProcedure_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _generator.GenerateProcedureScript(null!));
        }

        [Test]
        public void GenerateProcedureScript_WithEmptySourceCode_ThrowsInvalidOperationException()
        {
            var procedure = new Procedure
            {
                Name = "TEST_PROC",
                SourceCode = ""
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _generator.GenerateProcedureScript(procedure));
            Assert.That(ex.Message, Does.Contain("has no source code"));
        }

        [Test]
        public void GenerateProcedureScript_WithSourceCodeEndingWithSemicolon_GeneratesCorrectScript()
        {
            var procedure = new Procedure
            {
                Name = "GET_USER",
                SourceCode = $"AS{Environment.NewLine}BEGIN{Environment.NewLine}  SELECT * FROM USERS;{Environment.NewLine}END;"
            };

            var result = _generator.GenerateProcedureScript(procedure);

            var expected = $"CREATE PROCEDURE GET_USER{Environment.NewLine}AS{Environment.NewLine}BEGIN{Environment.NewLine}  SELECT * FROM USERS;{Environment.NewLine}END;";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void GenerateProcedureScript_WithSourceCodeWithoutSemicolon_AddsSemicolon()
        {
            var procedure = new Procedure
            {
                Name = "GET_USER",
                SourceCode = $"AS{Environment.NewLine}BEGIN{Environment.NewLine}  SELECT * FROM USERS{Environment.NewLine}END;"
            };

            var result = _generator.GenerateProcedureScript(procedure);

            Assert.That(result, Does.EndWith(";"));
            Assert.That(result, Does.StartWith("CREATE PROCEDURE GET_USER"));
        }

        [Test]
        public void SaveToFiles_WithEmptyOutputDirectory_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                _generator.SaveToFiles("", new List<Domain>(), new List<Table>(), new List<Procedure>()));

            Assert.That(ex.ParamName, Is.EqualTo("outputDirectory"));
        }

        [Test]
        public void SaveToFiles_WithNonExistentDirectory_CreatesDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                var domain = new Domain { Name = "D_TEST", DataType = "INTEGER" };
                _generator.SaveToFiles(tempDir, new List<Domain> { domain }, new List<Table>(), new List<Procedure>());

                Assert.That(Directory.Exists(tempDir), Is.True);
                Assert.That(Directory.Exists(Path.Combine(tempDir, "domains")), Is.True);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void SaveToFiles_WithDomains_CreatesCorrectFiles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                var domains = new List<Domain>
                {
                    new Domain { Name = "D_ID", DataType = "INTEGER", IsNullable = false },
                    new Domain { Name = "D_NAME", DataType = "VARCHAR(100)" }
                };

                _generator.SaveToFiles(tempDir, domains, new List<Table>(), new List<Procedure>());

                var domainsDir = Path.Combine(tempDir, "domains");
                Assert.That(File.Exists(Path.Combine(domainsDir, "D_ID.sql")), Is.True);
                Assert.That(File.Exists(Path.Combine(domainsDir, "D_NAME.sql")), Is.True);

                var content = File.ReadAllText(Path.Combine(domainsDir, "D_ID.sql"));
                Assert.That(content, Does.Contain("CREATE DOMAIN D_ID"));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void SaveToFiles_WithTables_CreatesCorrectFiles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                var tables = new List<Table>
                {
                    new Table
                    {
                        Name = "USERS",
                        Columns = new List<Column>
                        {
                            new Column { Name = "ID", DataType = "INTEGER", IsNullable = false, Position = 0 }
                        }
                    }
                };

                _generator.SaveToFiles(tempDir, new List<Domain>(), tables, new List<Procedure>());

                var tablesDir = Path.Combine(tempDir, "tables");
                Assert.That(File.Exists(Path.Combine(tablesDir, "USERS.sql")), Is.True);

                var content = File.ReadAllText(Path.Combine(tablesDir, "USERS.sql"));
                Assert.That(content, Does.Contain("CREATE TABLE USERS"));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Test]
        public void SaveToFiles_WithProcedures_CreatesCorrectFiles()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                var procedures = new List<Procedure>
                {
                    new Procedure
                    {
                        Name = "GET_USER",
                        SourceCode = "AS BEGIN SELECT * FROM USERS; END;"
                    }
                };

                _generator.SaveToFiles(tempDir, new List<Domain>(), new List<Table>(), procedures);

                var proceduresDir = Path.Combine(tempDir, "procedures");
                Assert.That(File.Exists(Path.Combine(proceduresDir, "GET_USER.sql")), Is.True);

                var content = File.ReadAllText(Path.Combine(proceduresDir, "GET_USER.sql"));
                Assert.That(content, Does.Contain("CREATE PROCEDURE GET_USER"));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}
