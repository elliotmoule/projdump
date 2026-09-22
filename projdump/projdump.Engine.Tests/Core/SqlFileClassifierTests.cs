using projdump.Engine.Core;
using projdump.Engine.Tests.TestSupport;

namespace projdump.Engine.Tests.Core;

[TestFixture]
public class SqlFileClassifierTests
{
	// Anchored under the temp fixture root so path-segment checks never see the test runner's CWD.
	static readonly DirectoryInfo RootDir = new(FakePaths.Combine("MyApp"));

	static FileInfo RootFile(string fileName) => new(FakePaths.Combine("MyApp", fileName));

	static FileInfo NestedFile(params string[] segments) => new(FakePaths.Combine([.. new[] { "MyApp" }, .. segments]));

	[TestCase("schema.sql")]
	[TestCase("SeedData.SQL")]
	public void IsSqlFile_ReturnsTrue_ForSqlExtension(string fileName)
	{
		Assert.That(SqlFileClassifier.IsSqlFile(RootFile(fileName)), Is.True);
	}

	[TestCase("OrderService.cs")]
	[TestCase("deploy.ps1")]
	[TestCase("appsettings.json")]
	public void IsSqlFile_ReturnsFalse_ForEverythingElse(string fileName)
	{
		Assert.That(SqlFileClassifier.IsSqlFile(RootFile(fileName)), Is.False);
	}

	[Test]
	public void SqlFilePriority_ReturnsZero_ForRootLevelFile()
	{
		Assert.That(SqlFileClassifier.SqlFilePriority(RootFile("schema.sql"), RootDir), Is.EqualTo(0));
	}

	[TestCase("sql")]
	[TestCase("Database")]
	[TestCase("StoredProcedures")]
	[TestCase("seeds")]
	public void SqlFilePriority_ReturnsOne_ForConventionalSqlFolder(string folderName)
	{
		Assert.That(SqlFileClassifier.SqlFilePriority(NestedFile(folderName, "schema.sql"), RootDir), Is.EqualTo(1));
	}

	[Test]
	public void SqlFilePriority_ReturnsOne_ForConventionalFolderNestedDeeper()
	{
		Assert.That(SqlFileClassifier.SqlFilePriority(NestedFile("src", "db", "reports", "monthly.sql"), RootDir), Is.EqualTo(1));
	}

	[Test]
	public void SqlFilePriority_ReturnsTwo_ForFileElsewhere()
	{
		Assert.That(SqlFileClassifier.SqlFilePriority(NestedFile("src", "Reporting", "monthly.sql"), RootDir), Is.EqualTo(2));
	}
}