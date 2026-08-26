using Oficina.Api.Configuration;

namespace Oficina.Tests.Api;

public sealed class DotEnvLoaderTests
{
    private static readonly string[] Keys =
    [
        "OFICINA_TEST_SIMPLE",
        "OFICINA_TEST_EXPORTED",
        "OFICINA_TEST_DOUBLE_QUOTED",
        "OFICINA_TEST_SINGLE_QUOTED",
        "OFICINA_TEST_EXISTING",
    ];

    [Fact]
    public void LoadFromProjectRoot_should_parse_env_file_and_preserve_existing_variables()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("oficina-dotenv-tests-");
        var originalDirectory = Environment.CurrentDirectory;

        try
        {
            File.WriteAllLines(Path.Combine(tempDirectory.FullName, ".env"),
            [
                "# comment line, should be ignored",
                "",
                "OFICINA_TEST_SIMPLE=value1",
                "export OFICINA_TEST_EXPORTED=value2",
                "OFICINA_TEST_DOUBLE_QUOTED=\"hello world\"",
                "OFICINA_TEST_SINGLE_QUOTED='hello single'",
                "OFICINA_TEST_EXISTING=should_not_override",
            ]);

            Environment.SetEnvironmentVariable("OFICINA_TEST_EXISTING", "original");
            Environment.CurrentDirectory = tempDirectory.FullName;

            DotEnvLoader.LoadFromProjectRoot();

            Assert.Equal("value1", Environment.GetEnvironmentVariable("OFICINA_TEST_SIMPLE"));
            Assert.Equal("value2", Environment.GetEnvironmentVariable("OFICINA_TEST_EXPORTED"));
            Assert.Equal("hello world", Environment.GetEnvironmentVariable("OFICINA_TEST_DOUBLE_QUOTED"));
            Assert.Equal("hello single", Environment.GetEnvironmentVariable("OFICINA_TEST_SINGLE_QUOTED"));
            Assert.Equal("original", Environment.GetEnvironmentVariable("OFICINA_TEST_EXISTING"));
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            foreach (var key in Keys)
            {
                Environment.SetEnvironmentVariable(key, null);
            }

            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void LoadFromProjectRoot_should_do_nothing_when_no_env_file_is_found()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("oficina-dotenv-tests-empty-");
        var originalDirectory = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = tempDirectory.FullName;

            var exception = Record.Exception(DotEnvLoader.LoadFromProjectRoot);

            Assert.Null(exception);
        }
        finally
        {
            Environment.CurrentDirectory = originalDirectory;
            tempDirectory.Delete(recursive: true);
        }
    }
}
