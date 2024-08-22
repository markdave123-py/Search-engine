using SearchEngine.Models;
using MongoDB.Driver;
using SearchEngine.Contexts;
using SearchEngine.Api.Core.Interfaces;
using SearchEngine.Api.Core.Files;
using System.Linq;

namespace SearchEngine.Api.Core.Services
{
    public class DocumentService: IDocumentService
    {
        private readonly MongoDBContext _context;
    private readonly FileManager _fileManager;

    public DocumentService(MongoDBContext context, FileManager fileManager)
        {
            _context = context;
            _fileManager = fileManager;
    }

        public async Task AddDocumentAsync(Document document)
        {
            await _context.Documents.InsertOneAsync(document);
        }

        public async Task<List<Document>> GetUnIndexedDocumentsAsync()
        {
            return await _context.Documents.Find(d => !d.IsIndexed).ToListAsync();
        }

        public async Task UpdateDocumentIndexStatus(string docId)
        {
            var filter = Builders<Document>.Filter.Eq(d => d.ID, docId);
            var update = Builders<Document>.Update.Set(d => d.IsIndexed, true);
            await _context.Documents.UpdateOneAsync(filter, update);
        }

        public async Task IndexDocumentsAsync()
        {
            // Fetch unindexed documents
            var unIndexedDocuments = await GetUnIndexedDocumentsAsync();

            foreach (var document in unIndexedDocuments)
            {
                var nonStopWords = _fileManager.RemoveStopWordsAndPunctuation(document.Content.ToString());

                // Get base words from document (assumed to be provided by an external algorithm)
                var baseWords = GetBaseWordsFromDocument(nonStopWords);

                foreach (var word in baseWords)
                {
                    var wordMatch = new WordIndexer
                    {
                        Word = word,
                        Matches = new List<Match>()
                    };

                    // Go through all documents and find matches
                    var allDocuments = await _context.Documents.Find(_ => true).ToListAsync();
                    foreach (var doc in allDocuments)
                    {
                        var positions = GetWordPositionsInDocument(doc.Content, word);
                        if (positions.Any())
                        {
                            wordMatch.Matches.Add(new Match
                            {
                                DocId = doc.ID,
                                Positions = positions
                            });
                        }
                    }

                    // Store the word and its matches in the WordIndexer collection
                    await _context.WordIndexer.InsertOneAsync(wordMatch);
                }

                // Mark document as indexed
                await UpdateDocumentIndexStatus(document.ID);
            }
        }

        // Simulated method to extract base words (assumed to be provided)
        private List<string> GetBaseWordsFromDocument(string[] content)
        {
      // Assume this method returns a list of base words
      return content.ToList(); // Replace with actual implementation
    }

        // Method to get positions of a word in the document content
        private List<int> GetWordPositionsInDocument(List<string> content, string word)
        {
            var positions = new List<int>();
            for (int i = 0; i < content.Count; i++)
            {
                if (content[i] == word)
                {
                    positions.Add(i);
                }
            }
            return positions;
        }
    }
}
