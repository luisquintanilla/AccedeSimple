using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace AccedeSimple.Service.Services;

public class IngestionService
{
    private readonly VectorStoreCollection<int, Document> _collection;
    public IngestionService(VectorStoreCollection<int, Document> collection)
    {
        _collection = collection;
    }

    public async Task IngestAsync(string sourceDirectory)
    {
        await _collection.EnsureCollectionExistsAsync();
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.pdf");
        foreach (var file in sourceFiles)
        {
            using var pdf = PdfDocument.Open(Path.Combine(sourceDirectory, file));
            var paragraphs = pdf.GetPages().SelectMany(GetPageParagraphs).ToList();

            var documents =
                paragraphs
                    .Select((p, i) =>
                    {
                        return new Document
                        {
                            Id = i,
                            FileName = Path.GetFileName(file),
                            PageNumber = p.PageNumber,
                            IndexOnPage = p.IndexOnPage,
                            Embedding = p.Text
                        };
                    });

            await _collection.UpsertAsync(documents);
        }
    }

    private IEnumerable<(int PageNumber, int IndexOnPage, string Text)> GetPageParagraphs(Page pdfPage)
    {
        var letters = pdfPage.Letters;
        var words = NearestNeighbourWordExtractor.Instance.GetWords(letters);
        var textBlocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
        var pageText = string.Join(Environment.NewLine + Environment.NewLine,
            textBlocks.Select(t => t.Text.ReplaceLineEndings(" ")));

        #pragma warning disable SKEXP0050
        return TextChunker.SplitPlainTextParagraphs([pageText], 200)
            .Select((text, index) => (pdfPage.Number, index, text));
        #pragma warning restore SKEXP0050
    }

}

public record class Document
{
    [VectorStoreKey]
    public int Id { get; set; }

    [VectorStoreData]
    public string FileName { get; set; }

    [VectorStoreData]
    public int PageNumber { get; set; }

    [VectorStoreData]
    public int IndexOnPage { get; set; }

    [VectorStoreVector(Dimensions: 1536)]
    public string? Embedding { get; set; }
}