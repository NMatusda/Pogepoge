using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class Program
    {
        private class Peculiarity
        {
            public string PeculiarityName { get; set; }
            public List<Pokemon> PokemonList { get; set; }
        }

        private class Pokemon
        {
            public string Name { get; set; }
            public bool IsDream { get; set; }
            public List<string> Pec { get; set; }
            public string DreamPec { get; set; }
        }

        public static void Main(string[] args)
        {
            string filepath = Console.ReadLine();
            string html = ReadHtml(filepath);
            var peclist = Parser(html);
            var pokelist = PokeParse(peclist);
            WriteCsv(filepath, pokelist);
        }

        private static string ReadHtml(string filePath)
        {
            using (var sr = new StreamReader(filePath, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        private static List<Peculiarity> Parser(string html)
        {
            var rtn = new List<Peculiarity>();
            var parser = new AngleSharp.Html.Parser.HtmlParser();
            var doc = parser.ParseDocument(html);
            var innernodes = doc.GetElementsByClassName("_inner");
            foreach (var innernode in innernodes)
            {
                var peculiarityname = innernode.GetElementsByClassName("_inner-header");
                var node1 = innernode.GetElementsByClassName("_inner-body");
                var node2 = node1[0].GetElementsByClassName("_toggle-body");
                var node3 = node2[0].GetElementsByClassName("_mini-pokemon-list");
                var pokelist = node3[0].GetElementsByClassName("js-characteristic-has-pokemon");
                rtn.Add(new Peculiarity()
                {
                    PeculiarityName = peculiarityname[0].TextContent,
                    PokemonList = pokelist.Select(x =>
                    {
                        return new Pokemon()
                        {
                            Name = x.QuerySelectorAll("span").First().TextContent,
                            IsDream = x.GetElementsByClassName("_dream").Any(),
                        };
                    }).ToList(),
                });
            }

            return rtn;
        }

        private static List<Pokemon> PokeParse(List<Peculiarity> pecList)
        {
            var pokelist = new List<Pokemon>();
            // ポケモンの一覧作成
            pecList.ForEach(x =>
            {
                var names = x.PokemonList.Select(y => y.Name).Distinct();
                var pokenamelist = pokelist.Select(y => y.Name).Distinct();
                foreach (var name in names)
                {
                    if (!pokenamelist.Contains(name))
                    {
                        pokelist.Add(new Pokemon()
                        {
                            Name = name,
                            Pec = new List<string>(),
                        });
                    }
                }
            });
            // 特性を流し込む
            pecList.ForEach(x =>
            {
                // 通常特性
                var targetpoke = x.PokemonList.Where(y => !y.IsDream).Select(y => y.Name);
                foreach (var poke in pokelist.Where(y => targetpoke.Contains(y.Name)))
                {
                    poke.Pec.Add(x.PeculiarityName);
                }
                // 夢特性
                targetpoke = x.PokemonList.Where(y => y.IsDream).Select(y => y.Name);
                foreach (var poke in pokelist.Where(y => targetpoke.Contains(y.Name)))
                {
                    poke.DreamPec = x.PeculiarityName;
                }
            });

            return pokelist;
        }

        private static void WriteCsv(string inHtmlFilePath, List<Pokemon> pokelist)
        {
            var fi = new FileInfo(inHtmlFilePath);
            var maxpeccnt = pokelist.Max(x => x.Pec.Count);
            using (var sw = new StreamWriter(Path.Combine(fi.DirectoryName, $"{fi.Name.Replace(fi.Extension, "")}.csv")))
            {
                foreach (var poke in pokelist.OrderBy(x => x.Name))
                {
                    for (int i = poke.Pec.Count; i < maxpeccnt; i++)
                    {
                        poke.Pec.Add(string.Empty);
                    }
                    sw.WriteLine($"{poke.Name},{string.Join(",", poke.Pec)},{poke.DreamPec}");
                }
            }
        }
    }
}
