using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ViginereBreaker
{
    class Program
    {
        const string CipherText = "XHWLISAMYALBSNKTWRWVELDXHBQIPALHENVTWVAOMDDRECLXHUHHRBQVSRLXDHSOIAUHQMGGENVBRTWKISLBRGMGHEJECIFZPOYBGNGMMCWMLALMLEKHPDAXVSSKINGMQOLBZALXHTGKITJXETBNWTGKIVWGQAAGPYTRXHWBVRSMMOFTPAKLISKFINLHJTZXHAFZIRKHJBSMXLWTRDTRXHWBVSWEJIFMIRWLXRSMLEJMLEQWMSUHZEJTWOMGHRWTWOFMSRMGEWSRFYJXELASMNYMLALPLALBXMSDISKXRSWYSRLAIMLHHOVXTEFWWOFPLALBXWAEPMSDISWGWEXHVOLAIRKMSDGTRDLAETSEPOXMLEGMLEJLGAFGSTAVITZBWTGHIVWGEQMBXETKEVWLSLVBIRETCPJXJEJMSRMGVALAIRLAENZXVOAVELDRFULISIFMPEKLPYVBITJRMNYMSSLXQTZXSNUHQIFZXIVXELDUCHAFWEDYXHMLAEUHYLVBQAYBREOBXHGNXCGGXRSWMCLBSNSVMRUNQSLTRCWBRWZBGHSGERERELDHJWZHWEEXQBWKWAJXFRSOIFDXISSMXOHLTEWWFEXHVELAIEFXQYETOEKTQONXMFLAISGEHIWKWRWTPLQTVETKEVWMLEFMLIKLYRWECIKGXTZXSULVSMWTRYGYXHWFAAFMIDWTGHOHYLVAEVWIVEXXVRWWXHSMELDLXAFWENVYMGZMAHSMAEZTZEZXVELAINALECSLIIFPLIUAXHWBRTWKECLBSNGYQAFRMNVBZIVNELDRVALBSNSEHEUBWIGGQACBRGHKSCWLWEKHREHKSCWLWPWKWODWMEJIVOVNGEKTROMMGOEXMNLXRDWWFYFHSNWFSSLTVMAXWTJRXOSOSIVMLIKIVOTEIMBNWTSLGOJMIZVBHSAGGELAIYUTRTMLYADECMSDIRWMVESMTHQLMCSEPYAFTOKLMBDXXHWRQACXMTWVSNGFMCSEPYAFTOKLMBDXXHWRWHGHXDWLIRLXVSLAINKMENVBRGSGHFAZLTAGKIKXECZLSLVBIRKBRDAOMDMTPLQKETAHRADVSUJLIOXTGTAHRAXMIRSEPBWVEUKXXHWVSSLHJRMGRIFZMSKNVELHFESMPESLXAKAMGZTWTZXGOKMSFKMEYAGK";
        const int KeyLength = 4;
        const double CIdxConfideceTreshold = 0.057;

        const char AlphabetStartLetter = 'A';
        const int AlphabetLength = 26;

        static void Main(string[] args)
        {
            var chunks = SplitToPartial(CipherText, KeyLength);

            Console.WriteLine("Chunks based on the key length:");
            foreach (var chunk in chunks)
            {
                Console.WriteLine($"{chunk}");
            }

            var columns = Enumerable.Range(1, KeyLength).ChooseK(2).Select(c => c.ToArray()).ToArray();
            var offsets = chunks.ChooseK(2).Select(g =>
            {
                var group = g.ToArray();
                var potentialKey = FindLikelyKeyOffset(group[0], group[1]);
                if (potentialKey.Item2 > CIdxConfideceTreshold)
                {
                    return potentialKey.Item1;
                }

                return -1;
            }).Select((o, i) => new { Offset = -o, Columns = columns[i] }).ToArray();

            Console.WriteLine();
            Console.WriteLine("Calculated offsets based on the Mutual IC:");
            foreach (var offset in offsets)
            {
                Console.WriteLine($"Offset {offset.Offset,3}, between columns {offset.Columns[0],2} and {offset.Columns[1],2}.");
            }

            var builder = new StringBuilder();

            // First element always has offset of 0. Rest are formed as 1, 2; 1, 3; 1, 4...
            var offsetIndicies = new[] { 0 }.Concat(offsets.Select(c => c.Offset).Take(KeyLength - 1)).ToArray();
            var offsetChunks = chunks.Select((c, i) => OffsetText(c, -offsetIndicies[i])).ToArray();
            for (int i = 0; i < CipherText.Length; i++)
            {
                builder.Append(offsetChunks[i % KeyLength][i / KeyLength]);
            }
            var monoText = builder.ToString();

            // Guessing time.
            Console.WriteLine();
            Console.WriteLine("An answer may be visible. Key might give a clue.");
            var keySeed = offsetIndicies.Select(idc => OffsetChar(AlphabetStartLetter, idc));
            for (int i = 0; i < AlphabetLength; i++)
            {
                var item = new
                {
                    Key = OffsetText(keySeed, i),
                    Plaintext = OffsetText(monoText, -i)
                };
                Console.WriteLine($"Key: {item.Key}. Plaintext: {item.Plaintext}");
            }
        }

        /// <summary>
        /// Calculates index of coincidence. It is language/alphabet independant.
        /// </summary>
        /// <param name="text">Text to calculate index of coincidence of.</param>
        /// <returns>Index of coincidence.</returns>
        public static double CalculateIC(string text)
        {
            // Prepare text
            text = text.ToUpper();

            var charFrequency = AnalyzeCharFrequency(text);

            return charFrequency.Values.Aggregate(0.0, (acc, val) => { return acc + val * (val - 1); }) / (text.Length * (text.Length - 1));
        }

        /// <summary>
        /// Tries to find the cypher key offset between 2 cyphertexts.
        /// </summary>
        /// <param name="t1">Text 1 (a1)</param>
        /// <param name="t2">Text 1 (a1 + z)</param>
        /// <returns>Most likely value for z, and its index of coincidence.<returns>
        public static (int, double) FindLikelyKeyOffset(string t1, string t2)
        {
            var offset = 0;
            var coincidenceIdx = 0.0;
            for (int i = 0; i < AlphabetLength; i++)
            {
                // Add +1 to the alphabet. Loop around if it exceeds last letter.
                var newIc = CalculateMutualIC(t1, OffsetText(t2, i + 1));
                if (newIc > coincidenceIdx)
                {
                    offset = i + 1;
                    coincidenceIdx = newIc;
                }
            }

            return (offset, coincidenceIdx);
        }

        public static char OffsetChar(char c, int offset)
        {
            return (char)((c + offset + AlphabetLength - AlphabetStartLetter) % AlphabetLength + AlphabetStartLetter);
        }

        public static string OffsetText(IEnumerable<char> text, int offset)
        {
            return new string(text.Select(c => OffsetChar(c, offset)).ToArray());
        }

        /// <summary>
        /// Calculates the mutual index of coincidence.
        /// T1 and T2 must me normalized (all uppercase or all lowercase).
        /// </summary>
        /// <param name="t1">Text 1</param>
        /// <param name="t2">Text 2</param>
        /// <returns>Mutual index of coincidence.</returns>
        public static double CalculateMutualIC(string t1, string t2)
        {
            var f1 = AnalyzeCharFrequency(t1);
            var f2 = AnalyzeCharFrequency(t2);

            var micTotal = Enumerable.Range(AlphabetStartLetter, AlphabetLength).Aggregate(0.0, (acc, val) =>
            {
                // If one or both of the letters do not exist, the product of multiplication equates to 0.
                if (f1.TryGetValue((char)val, out var fi1) && f2.TryGetValue((char)val, out var fi2))
                {
                    return acc + fi1 * fi2;
                }

                return acc;
            });

            return micTotal / (t1.Length * t2.Length);
        }

        public static Dictionary<char, int> AnalyzeCharFrequency(string text)
        {
            var charFrequency = new Dictionary<char, int>();

            foreach (var ch in text)
            {
                if (!charFrequency.ContainsKey(ch))
                {
                    charFrequency.Add(ch, 0);
                }

                charFrequency[ch]++;
            }

            return charFrequency;
        }

        public static string[] SplitToPartial(string fullText, int keyLength)
        {
            var builders = Enumerable.Range(0, keyLength).Select((i) => new StringBuilder()).ToArray();
            for (int i = 0; i < fullText.Length; i++)
            {
                builders[i % keyLength].Append(fullText[i]);
            }

            return builders.Select(b => b.ToString()).ToArray();
        }
    }
}
