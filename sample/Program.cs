using System;
using Hnx8.BregonigDotNet;

namespace Sample
{
    /// <summary>
    /// BregonigDotNet 動作サンプルプログラム
    /// </summary>
    /// <remarks>
    /// このProgram.csと同じフォルダにbregonig.dllを配置してビルド・実行してください。
    /// </remarks>
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // 正規表現検索サンプルを実行
                ExecMatchSample();
                // 正規表現置換サンプルを実行
                ExecReplaceSample();
            }
            catch (DllNotFoundException ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine
                    + "dllがありません。実行フォルダにbregonig.dllを配置してください。");
            }
            catch (BadImageFormatException ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine
                    + "dllの32bit/64bitが妥当ではない可能性もあります。正しいbregonig.dllを実行フォルダに配置してください。");
            }
            catch (TypeLoadException ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine
                    + "正しいbregonig.dllを実行フォルダに配置してください。");
            }
            Console.ReadLine();
        }

        /// <summary>
        /// 正規表現検索を実行して結果を表示するサンプルメソッドです。
        /// </summary>
        static void ExecMatchSample()
        {
            // 以下のソースコードで使用しているメソッドのほか、Matches()/IsMatch()メソッドやstaticのメソッドも使用可能です。
            // 使い方は、System.Text.RegularExpressions.Regexクラスで提供されている同名のメソッドとほぼ同一です。

            // 検索サンプル１
            try
            {
                string pattern = @"(03|045)-(\d{3,4})-(\d{4})";
                Console.WriteLine("pattern:「" + pattern + "」");
                // コンストラクタで正規表現パターンを指定します。
                using (BregonigRegex regex = new BregonigRegex(pattern))
                {
                    string text = " Yokohama 045-222-1111  Osaka 06-5555-6666  Tokyo 03-1111-9999 ";
                    Console.WriteLine("input:「" + text + "」");
                    // Match()メソッドで検索文字列を指定し、検索を行います。
                    BregonigRegex.Matched match = regex.Match(text);
                    while (match != null)
                    {
                        // 検索マッチ箇所（全体）は、RegularExpressionsのMatchクラスと同じ要領で参照できます。
                        Console.WriteLine(" ->found:「" + match.Value + "」"
                            + "(start=" + match.Index + ", length=" + match.Length + ")");
                        // 同一検索文字列について次のマッチ箇所を探す場合はNextMatch()を使用します。
                        match = match.NextMatch();
                    }
                }
                // オブジェクトの解放はusing(Dispose)により行われます。
            }
            catch (ArgumentException ex)
            {
                // 正規表現にエラーがある場合はArgumentExceptionが投げられます
                Console.WriteLine(" ->error:「" + ex.Message + "」");
            }
            Console.WriteLine();

            // 検索サンプル２（キャプチャグループ）
            try
            {
                string pattern = @"(AM|PM)?(\d{1,2}):(\d{2})";
                Console.WriteLine("pattern:「" + pattern + "」/i");
                // コンストラクタ引数２で正規表現オプションも指定できます。
                using (BregonigRegex regex = new BregonigRegex(pattern, "i"))
                {
                    string text = "AM0:00 AM10:00 pm3:00 pm2.5 24:00 1:1 0:00";
                    Console.WriteLine("input:「" + text + "」(beginning:5, length:text.Length-8)");
                    // Matchメソッドは開始位置/終了位置の指定もできます。
                    BregonigRegex.Matched match = regex.Match(text, 5, text.Length - 8);
                    while (match != null)
                    {
                        Console.WriteLine(" ->found:「" + match.Value + "」"
                            + "(start=" + match.Index + ", length=" + match.Length + ")");
                        // キャプチャグループは、RegularExpressionsのGroupクラスと同じ要領で参照できます。
                        var groups = match.Groups;
                        for (int i = 0; i < groups.Count; i++)
                        {
                            // ただしキャプチャされていないグループはnullになります
                            if (groups[i] != null)
                            {
                                Console.WriteLine("   [" + i + "]: 「" + groups[i].Value + "」"
                                    + "(start=" + groups[i].Index + ", length=" + groups[i].Length + ")");
                            }
                            else
                            {
                                Console.WriteLine("   [" + i + "]: （null）");
                            }
                        }
                        match = match.NextMatch();
                    }
                }
            }
            catch (ArgumentException ex) { Console.WriteLine(" ->error:「" + ex.Message + "」"); }
            Console.WriteLine();

            // 検索サンプル３（検索中でのパターン記憶の例）
            try
            {
                string pattern = @"abc(.)abc\1";
                Console.WriteLine("pattern:「" + pattern + "」");
                // 正規表現パターン内でキャプチャしたグループの文字列指定（前方参照）も使用できます。
                using (BregonigRegex regex = new BregonigRegex(pattern))
                {
                    string text = " abcdabce abcdabcd abcdabcf abcgabcg ";
                    Console.WriteLine("input:「" + text + "」");

                    BregonigRegex.Matched match = regex.Match(text);
                    while (match != null)
                    {
                        Console.WriteLine(" ->found:「" + match.Value + "」"
                            + "(start=" + match.Index + ", length=" + match.Length + ")");
                        var groups = match.Groups;
                        Console.WriteLine("   [1]: " + groups[1].Value
                            + "(start=" + groups[1].Index + ", length=" + groups[1].Length + ")");
                        match = match.NextMatch();
                    }
                }
            }
            catch (ArgumentException ex) { Console.WriteLine(" ->error:「" + ex.Message + "」"); }
            Console.WriteLine();

            // 検索サンプルＸ（正規表現エラーの場合）
            try
            {
                string pattern = @"***";
                Console.WriteLine("pattern:「" + pattern + "」");
                // 正規表現にエラーがある場合はArgumentExceptionが投げられます
                using (BregonigRegex regex = new BregonigRegex(pattern))
                {
                    string text = " 正規表現パターンにエラーがある場合 ";
                    Console.WriteLine("input:「" + text + "」");
                    BregonigRegex.Matched match = regex.Match(text);
                }
            }
            catch (ArgumentException ex)
            {
                // ArgumentExceptionは適切に処理してください。
                Console.WriteLine(" ->error:「" + ex.Message + "」");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 正規表現置換を実行して結果を表示するサンプルメソッドです。
        /// </summary>
        static void ExecReplaceSample()
        {
            // 以下のソースコードで使用しているメソッドのほか、Match.Result()メソッドも使用可能です。
            // 使い方は、System.Text.RegularExpressions.Regexクラスで提供されている同名のメソッドとほぼ同一です。

            // 置換サンプル１："g"つき
            try
            {
                string pattern = @"(\d\d)-\d{4}-\d{4}";
                Console.WriteLine("pattern:「" + pattern + "」/gあり");
                // コンストラクタで正規表現パターンを指定します。
                using (BregonigRegex regex = new BregonigRegex(pattern, "g"))
                {
                    string text = " Yokohama 045-222-1111  Osaka 06-5555-6666  Tokyo 03-1111-9999 ";
                    Console.WriteLine("input:「" + text + "」");
                    string replacement = "$1-xxxx-xxxx";
                    Console.WriteLine("replacement:「" + replacement + "」");
                    // Replace()メソッドで置換を行います。(gオプションにより一括)
                    string result = regex.Replace(text, replacement);
                    Console.WriteLine(" ->result:「" + result + "」");
                }
                // オブジェクトの解放はusing(Dispose)により行われます。
            }
            catch (ArgumentException ex) { Console.WriteLine(" ->error:「" + ex.Message + "」"); }
            Console.WriteLine();

            // 置換サンプル２："g"なし、staticメソッドによる置換
            try
            {
                string text = " Yokohama 045-222-1111  Osaka 06-5555-6666  Tokyo 03-1111-9999 ";
                Console.WriteLine("input:「" + text + "」");
                string pattern = @"(\d\d)-\d{4}-\d{4}";
                Console.WriteLine("pattern:「" + pattern + "」/gなし");
                string replacement = "$1-xxxx-xxxx";
                Console.WriteLine("replacement:「" + replacement + "」");
                // staticのReplace()メソッドでも同様に置換できます（"g"オプションなし：最初の１件のみ置換）
                string result = BregonigRegex.Replace(text, pattern, replacement, "");
                Console.WriteLine(" ->result:「" + result + "」");
            }
            catch (ArgumentException ex) { Console.WriteLine(" ->error:「" + ex.Message + "」"); }
            Console.WriteLine();

            // 置換サンプルＸ（置換パターンエラーの場合）
            try
            {
                string pattern = @"[a-f]";
                Console.WriteLine("pattern:「" + pattern + "」");
                using (BregonigRegex regex = new BregonigRegex(pattern))
                {
                    string text = " 置換パターンにエラーがある場合0123456789abcdef ";
                    Console.WriteLine("input:「" + text + "」");
                    string replacement = null;
                    Console.WriteLine("replacement:(null)");
                    string result = regex.Replace(text, replacement);
                    Console.WriteLine(" ->result:「" + result + "」");
                }
            }
            catch (ArgumentException ex)
            {
                // ArgumentExceptionは適切に処理してください。
                Console.WriteLine(" ->error:「" + ex.Message + "」");
            }
            Console.WriteLine();
        }
    }
}
