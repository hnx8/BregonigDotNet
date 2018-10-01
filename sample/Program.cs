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
                    bool matchSuccess = regex.Match(text);
                    while (matchSuccess)
                    {
                        // 検索マッチ箇所（全体）は、RegularExpressionsのMatchクラスと同じ要領で参照できます。
                        Console.WriteLine(" ->found:「" + regex.Value + "」"
                            + "(start=" + regex.Index + ", length=" + regex.Length + ")");
                        // 同一検索文字列について次のマッチ箇所を探す場合はNextMatch()を使用します。
                        matchSuccess = regex.NextMatch();
                    }
                }
                // オブジェクトの解放はusing(Dispose)により行われます。
                Console.WriteLine();
            }
            catch (ArgumentException ex)
            {
                // 正規表現にエラーがある場合はArgumentExceptionが投げられます
                Console.WriteLine(" ->error:「" + ex.Message + "」");
            }

            // 検索サンプル２（キャプチャグループ）
            try
            {
                string pattern = @"(AM|PM)?(\d{1,2}):(\d{2})";
                Console.WriteLine("pattern:「" + pattern + "」/i");
                // コンストラクタ引数２で正規表現オプションも指定できます。
                using (BregonigRegex regex = new BregonigRegex(pattern, "i"))
                {
                    string text = " AM10:00 pm3:00 pm2.5 24:00 1:1 ";
                    Console.WriteLine("input:「" + text + "」(beginning:1, length:text.Length-2)");
                    // Matchメソッドは開始位置/終了位置の指定もできます。
                    bool matchSuccess = regex.Match(text, 1, text.Length - 2);
                    while (matchSuccess)
                    {
                        Console.WriteLine(" ->found:「" + regex.Value + "」"
                            + "(start=" + regex.Index + ", length=" + regex.Length + ")");
                        // キャプチャグループは、RegularExpressionsのGroupクラスと同じ要領で参照できます。
                        BregonigRegex.Group[] groups = regex.Groups;
                        for (int i = 0; i < groups.Length; i++)
                        {
                            // ただしキャプチャされていないグループはIndex=-1,Value=nullになります
                            string value = (groups[i].Value != null) ? "「" + groups[i].Value + "」" : " null ";
                            Console.WriteLine("   [" + i + "]: " + value
                                + "(start=" + groups[i].Index + ", length=" + groups[i].Length + ")");
                        }
                        matchSuccess = regex.NextMatch();
                    }
                }
                Console.WriteLine();
            }
            catch (ArgumentException ex) { Console.WriteLine(" ->error:「" + ex.Message + "」"); }

            // 検索サンプル３（検索中でのパターン記憶の例）
            try
            {
                string pattern = @"abc(.)abc\1";
                Console.WriteLine("pattern:「" + pattern + "」");
                // 鬼雲の正規表現では、正規表現パターン内でキャプチャグループの文字列も使用できます。
                using (BregonigRegex regex = new BregonigRegex(pattern))
                {
                    string text = " abcdabce abcdabcd abcdabcf abcgabcg ";
                    Console.WriteLine("input:「" + text + "」");
                    bool matchSuccess = regex.Match(text);
                    while (matchSuccess)
                    {
                        Console.WriteLine(" ->found:「" + regex.Value + "」"
                            + "(start=" + regex.Index + ", length=" + regex.Length + ")");
                        BregonigRegex.Group[] groups = regex.Groups;
                        Console.WriteLine("   [1]: " + groups[1].Value
                            + "(start=" + groups[1].Index + ", length=" + groups[1].Length + ")");
                        matchSuccess = regex.NextMatch();
                    }
                }
                Console.WriteLine();
            }
            catch (ArgumentException ex) { Console.WriteLine(" ->error:「" + ex.Message + "」"); }

            // 検索サンプル４（正規表現エラーの場合）
            try
            {
                string pattern = @"***";
                Console.WriteLine("pattern:「" + pattern + "」");
                // 正規表現にエラーがある場合はArgumentExceptionが投げられます
                using (BregonigRegex regex = new BregonigRegex(pattern))
                {
                    string text = " 正規表現パターンにエラーがある場合 ";
                    Console.WriteLine("input:「" + text + "」");
                    bool matchSuccess = regex.Match(text);
                }
            }
            catch (ArgumentException ex)
            {
                // ArgumentExceptionは適切に処理してください。
                Console.WriteLine(" ->error:「" + ex.Message + "」");
            }
        }
    }
}
