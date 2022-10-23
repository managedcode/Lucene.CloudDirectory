using System.Diagnostics;
using System.Text;
using Azure.Storage.Blobs;
using FluentAssertions;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Xunit;
using Xunit.Abstractions;

namespace ManagedCode.Lucene.CloudDirectory.Tests;

public class CloudDirectoryTests
{
    private static readonly Random Random = new();

    private static readonly string[] SampleTerms =
    {
        "dog", "cat", "car", "horse", "door", "tree", "chair", "microsoft", "apple", "adobe", "google", "golf",
        "linux", "windows", "firefox", "mouse", "hornet", "monkey", "giraffe", "computer", "monitor",
        "steve", "fred", "lili", "albert", "tom", "shane", "gerald", "chris",
        "love", "hate", "scared", "fast", "slow", "new", "old"
    };

    private readonly ITestOutputHelper _outputHelper;

    private readonly string connectionString =
        "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1;";

    public CloudDirectoryTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task Lucene()
    {
        var sw = Stopwatch.StartNew();
        const string containerName = "lucene";
        var blobClient = new BlobServiceClient(connectionString);
        var container = blobClient.GetBlobContainerClient(containerName);
        //container.DeleteIfExists();
        await container.CreateIfNotExistsAsync();

        var azureDirectory = new CloudDirectory(connectionString, containerName);

        var ll = Stopwatch.StartNew();
        var (dog, cat, car) = InitializeCatalog(azureDirectory, 100_000);
        ll.Stop();
        _outputHelper.WriteLine($"InitializeCatalog:{ll.Elapsed}");

        try
        {
            var ll2 = Stopwatch.StartNew();
            var ireader = DirectoryReader.Open(azureDirectory);
            var searcher = new IndexSearcher(ireader);
            for (var i = 0; i < 10_000; i++)
            {
                var searchForPhrase = SearchForPhrase(searcher, "dog");
                dog.Should().Be(searchForPhrase);
                searchForPhrase = SearchForPhrase(searcher, "cat");
                cat.Should().Be(searchForPhrase);
                searchForPhrase = SearchForPhrase(searcher, "car");
                car.Should().Be(searchForPhrase);
            }

            Trace.TraceInformation("Tests passsed");
            ll2.Stop();
            _outputHelper.WriteLine($"DirectoryReader:{ll2.Elapsed}");
        }
        catch (Exception x)
        {
            _outputHelper.WriteLine(x.Message);
            Trace.TraceInformation("Tests failed:\n{0}", x);
        }
        finally
        {
            sw.Stop();
            _outputHelper.WriteLine($"All Time:{sw.Elapsed}");
            // check the container exists, and delete it
            container.Exists().Value.Should().BeTrue(); // check the container exists
            //container.Delete();
        }
    }

    [Fact(Skip = "only for manual testing")]
    public void TestReadAndWriteWithSubDirectory()
    {
        const string containerName = "testcatalogwithshards";
        var blobClient = new BlobServiceClient(connectionString);
        var container = blobClient.GetBlobContainerClient(containerName);
        container.CreateIfNotExists();

        var azureDirectory1 = new CloudDirectory(connectionString, $"{containerName}/shard1");
        var (dog, cat, car) = InitializeCatalog(azureDirectory1, 1000);
        var azureDirectory2 = new CloudDirectory(connectionString, $"{containerName}/shard2");
        var (dog2, cat2, car2) = InitializeCatalog(azureDirectory2, 500);

        ValidateDirectory(azureDirectory1, dog, cat, car);
        ValidateDirectory(azureDirectory2, dog2, cat2, car2);

        // delete all azureDirectory1 blobs
        foreach (var file in azureDirectory1.ListAll())
        {
            azureDirectory1.DeleteFile(file);
        }

        ValidateDirectory(azureDirectory2, dog2, cat2, car2);

        foreach (var file in azureDirectory2.ListAll())
        {
            azureDirectory2.DeleteFile(file);
        }
    }

    private static void ValidateDirectory(CloudDirectory azureDirectory2, int dog2, int cat2, int car2)
    {
        var ireader = DirectoryReader.Open(azureDirectory2);
        for (var i = 0; i < 100; i++)
        {
            var searcher = new IndexSearcher(ireader);
            var searchForPhrase = SearchForPhrase(searcher, "dog");
            dog2.Should().Be(searchForPhrase);
            searchForPhrase = SearchForPhrase(searcher, "cat");
            cat2.Should().Be(searchForPhrase);
            searchForPhrase = SearchForPhrase(searcher, "car");
            car2.Should().Be(searchForPhrase);
        }

        Trace.TraceInformation("Tests passsed");
    }

    private static (int dog, int cat, int car) InitializeCatalog(CloudDirectory azureDirectory, int docs)
    {
        var indexWriterConfig = new IndexWriterConfig(LuceneVersion.LUCENE_48,
            new StandardAnalyzer(LuceneVersion.LUCENE_48));

        //indexWriterConfig.MergePolicy.MaxCFSSegmentSizeMB = 5;

        var dog = 0;
        var cat = 0;
        var car = 0;
        using (var indexWriter = new IndexWriter(azureDirectory, indexWriterConfig))
        {
            for (var iDoc = 0; iDoc < docs; iDoc++)
            {
                var bodyText = GeneratePhrase(40);
                var doc = new Document
                {
                    new TextField("id", DateTime.Now.ToFileTimeUtc() + "-" + iDoc, Field.Store.YES),
                    new TextField("Title", GeneratePhrase(10), Field.Store.YES),
                    new TextField("Body", bodyText, Field.Store.YES)
                };
                dog += bodyText.Contains(" dog ") ? 1 : 0;
                cat += bodyText.Contains(" cat ") ? 1 : 0;
                car += bodyText.Contains(" car ") ? 1 : 0;
                indexWriter.AddDocument(doc);
            }

            Trace.TraceInformation("Total docs is {0}, {1} dog, {2} cat, {3} car", indexWriter.NumDocs, dog, cat, car);
        }

        return (dog, cat, car);
    }

    private static int SearchForPhrase(IndexSearcher searcher, string phrase)
    {
        var parser = new QueryParser(LuceneVersion.LUCENE_48, "Body", new StandardAnalyzer(LuceneVersion.LUCENE_48));
        var query = parser.Parse(phrase);
        var topDocs = searcher.Search(query, 100);
        return topDocs.TotalHits;
    }

    private static string GeneratePhrase(int maxTerms)
    {
        var phrase = new StringBuilder();
        var nWords = 2 + Random.Next(maxTerms);
        for (var i = 0; i < nWords; i++)
        {
            phrase.AppendFormat(" {0} {1}", SampleTerms[Random.Next(SampleTerms.Length)],
                Random.Next(32768).ToString());
        }

        return phrase.ToString();
    }
}