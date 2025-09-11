using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public struct WordInformation
    {
        public string Word { get; }
        public int Count { get; }
        public WordInformation(string word, int count) { Word = word; Count = count; }
        public override string ToString() => $"Word : {Word} Count : {Count}";
    }

    public sealed class TextManipulation : IDisposable
    {
        public string Path { get; }

        public TextManipulation(string path) => Path = path;

        public void AddTextToFile(string text)
        {
            using (var writer = File.AppendText(Path))
            {
                writer.WriteLine(text);
            }
        }

        public string ReadAllText()
        {
            using (var reader = File.OpenText(Path))
            {
                return reader.ReadToEnd();
            }
        }

        public List<WordInformation> CountWordsInText(params string[] args)
        {
            var allWords = ReadAllText()
                .Replace("\r\n", " ")
                .Replace("\r", " ")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var list = new List<WordInformation>();
            foreach (var w in args)
            {
                int n = allWords.Count(x => string.Equals(x, w, StringComparison.OrdinalIgnoreCase));
                list.Add(new WordInformation(w, n));
            }
            return list;
        }

        public void DeleteAllText() => File.WriteAllText(Path, string.Empty);

       
        public void Dispose(){}
    }
}


