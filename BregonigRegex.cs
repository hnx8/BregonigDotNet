using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Hnx8.BregonigDotNet
{
    /// <summary>
    /// bregonig.dllのUnicodeAPIを使用して鬼雲による正規表現検索を行うクラスです。
    /// </summary>
    public class BregonigRegex : IDisposable
    {
        ///////////////////////////////////////////////////////////////////////
        //
        // <BregonigRegex.cs>
        //   2018.10.01～  written by hnx8(H.Takahashi)
        //   https://github.com/hnx8/BregonigDotNet
        //
        // C#からbregonig.dllの鬼雲による正規表現を利用するためのクラスです。
        // http://k-takata.o.oo7.jp/mysoft/bregonig.html にて配布されている
        // bregonig.dll(Ver.3以降)を実行フォルダに配置して下さい。
        //
        // 利用可能な正規表現・オプション指定はbregonig.dllの仕様通りです。
        //  検索パターン http://k-takata.o.oo7.jp/mysoft/bregonig.html#SCT-5.1
        //  置換パターン http://k-takata.o.oo7.jp/mysoft/bregonig.html#SCT-5.3
        //  オプション   http://k-takata.o.oo7.jp/mysoft/bregonig.html#SCT-4.3.1-OPTIONS
        // を参照してください。
        // Unicode版APIを呼び出します。ANSI用オプションは使用できません。
        //
        ///////////////////////////////////////////////////////////////////////

        #region innerclass ----------------------------------------------------
        /// <summary>
        /// 正規表現にマッチした結果を表すクラスです。
        /// </summary>
        public class Matched
        {
            /// <summary>
            /// 正規表現にマッチした部分文字列の開始位置を返します。
            /// </summary>
            public int Index { get { return Groups[0].Index; } }

            /// <summary>
            /// 正規表現にマッチした部分文字列の長さを返します。
            /// </summary>
            public int Length { get { return Groups[0].Length; } }

            /// <summary>
            /// 正規表現にマッチした部分文字列を返します。
            /// </summary>
            public string Value { get { return Groups[0].Value; } }

            /// <summary>
            /// 正規表現マッチ箇所についてのキャプチャされたグループの配列を返します。
            /// [0]=マッチ箇所全体、[1]～[n]=グループ($1-$n)です。
            /// キャプチャされていないグループに対応する添字のオブジェクトはnullとなります。
            /// </summary>
            public ReadOnlyCollection<Group> Groups { get; protected set; }

            protected BregonigRegex regex; // grep実行時に指定されていた正規表現
            protected string input; // grep実行時に指定されていた検索テキスト
            protected int targetstart; // grep実行時に指定されていた検索開始文字位置
            protected int targetend; // grep実行時に指定されていた検索終了文字位置

            /// <summary>
            /// 新しいインスタンスを初期化します。（BregonigRegex内部用、独自に作成したコードから直接使用するものではありません）
            /// </summary>
            internal Matched(Group[] groups, BregonigRegex regex, int targetstart, int targetend)
            {
                this.Groups = new ReadOnlyCollection<Group>(groups);
                this.regex = regex;
                this.input = regex.input;
                this.targetstart = targetstart;
                this.targetend = targetend;
            }

            /// <summary>正規表現にマッチした部分文字列を返します。</summary>
            /// <returns></returns>
            public override string ToString() { return Value; }

            /// <summary>
            /// 一致する対象が最後に見つかった位置の終了位置から開始して、次に一致する対象を検索します。
            /// </summary>
            /// <returns>検索によって見つかった Matched オブジェクト。見つからなかった場合はnull</returns>
            /// <remarks>
            /// BregonigRegex正規表現オブジェクトがすでにDispose済の場合は、ObjectDisposedExceptionが投げられます。
            /// </remarks>
            public Matched NextMatch()
            {
                // このマッチ文字列以降の位置を探索対象とする。ただし長さ0のマッチだった場合は無限ループ回避のため次の位置
                int nextBeginning = Index + (Length == 0 ? 1 : Length);
                if (nextBeginning > targetend) { return null; }
                return regex.BoMatch(null, null, input, nextBeginning, targetend, false);
            }

            /// <summary>
            /// 正規表現にマッチした部分文字列を、指定した置換パターンで置換して返します。
            /// </summary>
            /// <param name="replacement">使用する置換パターン</param>
            /// <returns>置換パターン適用後の文字列</returns>
            /// <remarks>
            /// BregonigRegex正規表現オブジェクトがすでにDispose済の場合は、ObjectDisposedExceptionが投げられます。
            /// 置換パターンにエラーがある場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
            /// </remarks>
            public string Result(string replacement)
            {
                // このマッチ箇所以外は一括置換されないよう"g"オプションを外してBoSubst()を呼び出す
                return regex.BoSubst(replacement,
                    (regex.options.Contains("g") ? regex.options.Replace("g", "") : regex.options),
                    this.input,
                    this.targetstart,
                    this.targetend,
                    this); // 置換された部分のみを返す
            }
        }

        /// <summary>
        /// 正規表現に一致したグループを表すクラスです。
        /// </summary>
        public class Group
        {
            /// <summary>キャプチャした部分文字列の開始位置を返します。</summary>
            public readonly int Index;

            /// <summary>キャプチャした部分文字列の長さを返します。</summary>
            public readonly int Length;

            /// <summary>キャプチャした部分文字列を返します。</summary>
            public string Value { get { return input.Substring(Index, Length); } }

            protected readonly string input; // grep実行時に指定されていた検索テキスト

            /// <summary>
            /// 新しいインスタンスを初期化します。（BregonigRegex内部用、独自に作成したコードから直接使用するものではありません）
            /// </summary>
            internal Group(string input, int index, int length)
            {
                this.input = input;
                Index = index;
                Length = length;
            }

            /// <summary>キャプチャした部分文字列を返します。</summary>
            public override string ToString() { return Value; }
        }

        #endregion

        #region static method -------------------------------------------------
        /// <summary>
        /// 指定した入力文字列内で、指定した正規表現パターンと一致する箇所を検索します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="pattern">正規表現パターン文字列</param>
        /// <param name="options">正規表現オプション</param>
        /// <returns>検索によって見つかった Matched オブジェクト。見つからなかった場合はnull</returns>
        /// <remarks>
        /// 正規表現にエラーがある場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
        /// bregonig.dllの読み込みに失敗した場合は、DllNotFoundException(dllファイルがない)/TypeLoadException(BoMatchメソッドがない)/BadImageFormatException(x86/x64不一致)などの例外が投げられます。
        /// </remarks>
        public static Matched Match(string input, string pattern, string options = null)
        {
            using (BregonigRegex regex = new BregonigRegex(pattern, options))
            {
                return regex.Match(input);
            }
        }

        /// <summary>
        /// 指定した入力文字列内で、指定した正規表現パターンと一致する箇所を検索しすべて列挙します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="pattern">正規表現パターン文字列</param>
        /// <param name="options">正規表現オプション</param>
        /// <returns>検索によって見つかった Matched オブジェクトの列挙</returns>
        /// <remarks>
        /// 正規表現にエラーがある場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
        /// bregonig.dllの読み込みに失敗した場合は、DllNotFoundException(dllファイルがない)/TypeLoadException(BoMatchメソッドがない)/BadImageFormatException(x86/x64不一致)などの例外が投げられます。
        /// </remarks>
        public static IEnumerable<Matched> Matches(string input, string pattern, string options = null)
        {
            using (BregonigRegex regex = new BregonigRegex(pattern, options))
            {
                return regex.Matches(input);
            }
        }

        /// <summary>
        /// 指定した入力文字列内で、指定した正規表現パターンと一致する箇所が見つかるかどうかを返します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="pattern">正規表現パターン文字列</param>
        /// <param name="options">正規表現オプション</param>
        /// <returns>true:見つかった、false:見つからなかった</returns>
        /// <remarks>
        /// 正規表現にエラーがある場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
        /// bregonig.dllの読み込みに失敗した場合は、DllNotFoundException(dllファイルがない)/TypeLoadException(BoMatchメソッドがない)/BadImageFormatException(x86/x64不一致)などの例外が投げられます。
        /// </remarks>
        public static bool IsMatch(string input, string pattern, string options = null)
        {
            using (BregonigRegex regex = new BregonigRegex(pattern, options))
            {
                return regex.IsMatch(input);
            }
        }

        /// <summary>
        /// 指定した入力文字列内で指定した正規表現パターンと一致した箇所を、指定した置換文字列に置換します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="pattern">正規表現パターン文字列</param>
        /// <param name="replacement">置換文字列</param>
        /// <param name="options">正規表現オプション："g"(グローバルな置換)指定時は一致箇所すべて、"g"指定なしの場合は最初に一致した１箇所のみを、置換します）</param>
        /// <returns>置換結果文字列。正規表現パターンと一致した箇所がなかった場合は入力文字列と同一の文字列</returns>
        /// <remarks>
        /// 正規表現や置換パターンにエラーがある場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
        /// bregonig.dllの読み込みに失敗した場合は、DllNotFoundException(dllファイルがない)/TypeLoadException(BoMatchメソッドがない)/BadImageFormatException(x86/x64不一致)などの例外が投げられます。
        /// </remarks>
        public static string Replace(string input, string pattern, string replacement, string options = null)
        {
            using (BregonigRegex regex = new BregonigRegex(pattern, options))
            {
                return regex.Replace(input, replacement);
            }
        }
        #endregion

        #region constructor ---------------------------------------------------
        /// <summary>
        /// 鬼雲正規表現クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="pattern">正規表現パターン文字列</param>
        /// <param name="options">正規表現オプション（文字列で指定）</param>
        /// <remarks>
        /// アンマネージドリソースを使用します。使用後はDispose()してください。
        /// 正規表現にエラーがある場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
        /// bregonig.dllの読み込みに失敗した場合は、DllNotFoundException(dllファイルがない)/TypeLoadException(BoMatchメソッドがない)/BadImageFormatException(x86/x64不一致)などの例外が投げられます。
        /// </remarks>
        public BregonigRegex(string pattern, string options = null)
        {
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }

            this.pattern = pattern;
            this.options = options ?? ""; // 未指定時は空文字が指定されたとみなす
            this.prevoptions = this.options;
            this.rxp = IntPtr.Zero;
            try
            {
                BoMatch(pattern, options, "", 0, 0, true); // 正規表現を初期化
            }
            finally
            {
                FreeText(); // コンストラクタで例外となった場合もリソース解放は確実に実施する
            }
        }

        /// <summary>
        /// 鬼雲正規表現クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="pattern">正規表現パターン文字列</param>
        /// <param name="options">正規表現オプション（RegexOptionsで指定）</param>
        /// <param name="replaceGlobal">正規表現オプションとして"g"(グローバルな置換)を指定する場合true</param>
        /// <remarks>
        /// アンマネージドリソースを使用します。使用後はDispose()してください。
        /// 正規表現にエラーがある場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
        /// bregonig.dllの読み込みに失敗した場合は、DllNotFoundException(dllファイルがない)/TypeLoadException(BoMatchメソッドがない)/BadImageFormatException(x86/x64不一致)などの例外が投げられます。
        /// </remarks>
        public BregonigRegex(string pattern, RegexOptions options, bool replaceGlobal = false)
            : this(pattern, ConvertOption(options, replaceGlobal)) { }

        /// <summary>
        /// RegexOptionsで指定された正規表現オプションを鬼雲正規表現オプション文字列へ変換します。
        /// </summary>
        /// <param name="options">正規表現オプション（文字列で指定）</param>
        /// <param name="replaceGlobal">正規表現オプションとして"g"(グローバルな置換)を指定する場合true</param>
        /// <returns>鬼雲正規表現オプション文字列</returns>
        protected static string ConvertOption(RegexOptions options, bool replaceGlobal = false)
        {
            StringBuilder sb = new StringBuilder('u');
            if (options.HasFlag(RegexOptions.IgnoreCase)) { sb.Append('i'); }
            if (options.HasFlag(RegexOptions.Multiline)) { sb.Append('m'); }
            if (options.HasFlag(RegexOptions.Singleline)) { sb.Append('s'); }
            if (options.HasFlag(RegexOptions.IgnorePatternWhitespace)) { sb.Append('x'); }
            if (replaceGlobal) { sb.Append('g'); }
            return sb.ToString();
        }
        #endregion

        #region methods -------------------------------------------------------

        /// <summary>コンストラクタで指定された正規表現オプション文字列を返します。</summary>
        public string Options { get { return options; } }

        /// <summary>コンストラクタで指定された正規表現パターン文字列を返します。</summary>
        public override string ToString()
        {
            return pattern;
        }

        /// <summary>
        /// 指定した入力文字列内で、正規表現パターンと一致する箇所を検索します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="startat">検索開始文字位置（省略時は先頭から検索）</param>
        /// <returns>検索によって見つかった Matched オブジェクト。見つからなかった場合はnull</returns>
        public Matched Match(string input, int startat = 0)
        {
            return Match(input, startat, input.Length - startat);
        }

        /// <summary>
        /// 入力文字列の指定された開始位置・文字数の範囲で、正規表現パターンと一致する箇所を検索します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="beginning">検索開始文字位置</param>
        /// <param name="length">検索対象とする文字数</param>
        /// <returns>検索によって見つかった Matched オブジェクト。見つからなかった場合はnull</returns>
        /// <remarks>
        /// 入力文字列・開始位置・文字数が妥当ではない場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
        /// </remarks>
        public Matched Match(string input, int beginning, int length)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (beginning < 0 || beginning > input.Length)
            {
                throw new ArgumentOutOfRangeException("beginning", beginning, "");
            }
            if (length < 0 || beginning + length > input.Length)
            {
                throw new ArgumentOutOfRangeException("length", length, "");
            }
            //
            return BoMatch(null, null, input, beginning, beginning + length, false);
        }

        /// <summary>
        /// 指定した入力文字列内で、正規表現パターンと一致する箇所を検索しすべて列挙します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="startat">検索開始文字位置（省略時は先頭から検索）</param>
        /// <returns>検索によって見つかった Matched オブジェクトの列挙</returns>
        public IEnumerable<Matched> Matches(string input, int startat = 0)
        {
            Matched match = Match(input, startat);
            while (match != null)
            {
                yield return match;
                match = match.NextMatch();
            }
        }

        /// <summary>
        /// 指定した入力文字列内で、正規表現パターンと一致する箇所が見つかるかどうかを返します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="startat">検索開始文字位置（省略時は先頭から検索）</param>
        /// <returns>true:見つかった、false:見つからなかった</returns>
        public bool IsMatch(string input, int startat = 0)
        {
            return (Match(input, startat) != null);
        }

        /// <summary>
        /// 指定した入力文字列内で正規表現パターンと一致した箇所を、指定した置換パターンで置換します。
        /// </summary>
        /// <param name="input">検索・置換対象とする入力文字列</param>
        /// <param name="replacement">置換パターン</param>
        /// <param name="startat">入力文字列中での置換対象探索開始文字位置（省略時は先頭から置換対象を探索）</param>
        /// <returns>置換結果文字列。正規表現パターンと一致した箇所がなかった場合は入力文字列と同一の文字列</returns>
        /// <remarks>
        /// 正規表現オプション"g"(グローバルな置換)指定時は一致箇所すべて、"g"指定なしの場合は最初に一致した１箇所のみを、置換します。
        /// 置換パターンにエラーがある場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
        /// </remarks>
        public string Replace(string input, string replacement, int startat = 0)
        {
            return BoSubst(replacement, this.options, input, startat, input.Length);
        }
        #endregion

        #region disposing -----------------------------------------------------
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) { return; }

            // free managed objects
            if (disposing)
            {
                // none
            }
            // free unmanaged objects
            lock (pattern)
            {   // dll呼び出し中に案マネージドリソースを解放してしまわないようlockで排他制御
                FreeText();
                if (rxp != IntPtr.Zero)
                {
                    BRegfree(rxp);
                }
                rxp = IntPtr.Zero;
            }
            //
            disposed = true;
        }
        ~BregonigRegex()
        {
            Dispose(false);
        }

        #endregion

        #region protected -----------------------------------------------------
        /// <summary>コンストラクタで指定された正規表現パターン文字列</summary>
        protected readonly string pattern;
        /// <summary>コンストラクタで指定された正規表現オプション</summary>
        protected readonly string options;
        /// <summary>BREGEXP 構造体へのポインタ（要BRegFree解放）</summary>
        protected IntPtr rxp;

        /// <summary>直前の置換処理で指定された置換パターン</summary>
        protected string prevsubst;
        /// <summary>直前の置換処理で指定された正規表現オプション</summary>
        protected string prevoptions;

        /// <summary>入力文字列</summary>
        protected string input;
        /// <summary>入力文字列をアンマネージメモリに割り当てた先のポインタ（要Marshal解放）</summary>
        protected IntPtr strstartp;
        /// <summary>入力文字列をアンマネージメモリに割り当てます。</summary>
        /// <param name="input">入力文字列</param>
        protected void AllocText(string input)
        {
            // 検索対象の入力文字列が変わった場合のみアンマネージドメモリへ割り当て（変わっていないなら割り当てなおさない）
            if (!string.ReferenceEquals(input, this.input))
            {
                FreeText();
                this.strstartp = Marshal.StringToHGlobalUni(input);
                this.input = input;
            }
        }
        /// <summary>入力文字列をアンマネージメモリから解放します。</summary>
        protected void FreeText()
        {
            if (this.strstartp != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(strstartp);
                this.strstartp = IntPtr.Zero;
            }
        }

        /// <summary>
        /// bregexp(bregonig)が内部で使用するコンパイルブロック構造体
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        protected struct BREGEXP
        {
            public IntPtr outp;      // (result string start ptr)
            public IntPtr outendp;   // (result string end ptr)
            public int splitctr;     // (split result counter)
            public IntPtr splitpp;   // (split result pointer ptr)
            public int rsv1;         // (reserved for external use)
            public IntPtr parap;     // (parameter start ptr ie. "s/xxxxx/yy/gi")
            public IntPtr paraendp;  // (parameter end ptr)
            public IntPtr transtblp; // (translate table ptr)
            public IntPtr startpp;   // マッチしたデータの先頭ポインタ（の配列。[0]=マッチ箇所全体の開始位置、[1-n]=グループ($1-$n)の開始位置）
            public IntPtr endpp;     // マッチしたデータの最終ポインタ+1（の配列。[0]=マッチ箇所全体の終了位置、[1-n]=グループ($1-$n)の終了位置）
            public int nparens;      // パターンの中の() の数。 $1,$2,...,$n を調べるときに使用

            /// <summary>
            /// 引数で指定された添字のGroupオブジェクトを生成します。
            /// </summary>
            /// <param name="i">グループ添字：[0]=マッチ箇所全体、[1]～[n]=グループ($1-$n)</param>
            /// <param name="regex">正規表現オブジェクト</param>
            /// <returns>Groupオブジェクト。ただし該当Groupがキャプチャされていない場合はnull</returns>
            public Group CreateGroup(int i, BregonigRegex regex)
            {
                IntPtr startp = Marshal.ReadIntPtr(this.startpp, i * IntPtr.Size);
                IntPtr endp = Marshal.ReadIntPtr(this.endpp, i * IntPtr.Size);
                Group g = null;
                if (startp != IntPtr.Zero)
                {
                    g = new Group(regex.input,
                        (int)((startp.ToInt64() - regex.strstartp.ToInt64()) / sizeof(char)),
                        (int)((endp.ToInt64() - startp.ToInt64()) / sizeof(char)));
                }
                return g;
            }
        }

        /// <summary>
        /// BoMatch()呼び出し/結果処理簡略化
        /// </summary>
        /// <param name="pattern">検索パターン(NULLを指定すると、前回のコンパイル済みパターンを使用する)</param>
        /// <param name="options">オプション</param>
        /// <param name="input">検索対象文字列</param>
        /// <param name="targetstart">検索開始文字位置</param>
        /// <param name="targetend">検索終了文字位置</param>
        /// <param name="one_shot">ワンショットモード：通常はfalseを指定</param>
        /// <returns>検索によって見つかった Matched オブジェクト</returns>
        protected Matched BoMatch(
            string pattern,
            string options,
            string input,
            int targetstart,
            int targetend,
            bool one_shot)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("BregonigRegexオブジェクト");
            }

            lock (this.pattern)
            {
                // 検索位置をポインタに変換
                AllocText(input);
                IntPtr targetstartp = IntPtr.Add(strstartp, targetstart * sizeof(char));
                IntPtr targetendp = IntPtr.Add(strstartp, targetend * sizeof(char));

                StringBuilder msgSb = new StringBuilder(80); // エラーメッセージ格納用領域を確保
                int ret = BoMatch(pattern, options, strstartp, targetstartp, targetendp, one_shot, ref rxp, msgSb);
                if (ret < 0)
                {   // エラーあり
                    throw new ArgumentException(msgSb.ToString());
                }
                if (ret > 0)
                {
                    // マッチあり：構造体ポインタの最新の内容を取り出し
                    BREGEXP rx = (BREGEXP)(Marshal.PtrToStructure(rxp, typeof(BREGEXP)));
                    int arrayLength = rx.nparens + 1; // グループ数＋[0]マッチ箇所全体 の分
                    Group[] groups = new Group[arrayLength];
                    for (int i = 0; i < arrayLength; i++)
                    {
                        groups[i] = rx.CreateGroup(i, this);
                    }
                    return new Matched(groups, this, targetstart, targetend);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// BoSubst()呼び出し/結果処理簡略化
        /// </summary>
        /// <param name="subst">置換パターン(NULLを指定すると、前回のコンパイル済みパターンを使用する)</param>
        /// <param name="options">オプション</param>
        /// <param name="input">検索対象文字列</param>
        /// <param name="targetstart">検索開始文字位置</param>
        /// <param name="targetend">検索終了文字位置</param>
        /// <param name="match">置換対象となるMatchedオブジェクト</param>
        /// <returns>置換結果文字列（matchが指定されている場合はマッチした部分の変更結果、matchがnullの場合はinput全体の置換結果）</returns>
        protected string BoSubst(
            string subst,
            string options,
            string input,
            int targetstart,
            int targetend,
            Matched match = null)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("BregonigRegexオブジェクト");
            }
            // 置換オプション/置換パターンが前回と一緒であればコンパイルを省略する
            string pattern = this.pattern;
            if (options == this.prevoptions)
            {
                pattern = null;
                options = null;
                if (subst == this.prevsubst)
                {
                    subst = null;
                }
                else
                {   // subst相違あり、今回指定されたsubstを保存
                    this.prevsubst = subst;
                }
            }
            else
            {   // option相違あり、今回指定されたoptionsとsubstを保存
                this.prevoptions = options;
                this.prevsubst = subst;
            }

            lock (this.pattern)
            {
                // 検索位置をポインタに変換
                AllocText(input);
                IntPtr targetstartp = IntPtr.Add(strstartp, targetstart * sizeof(char));
                IntPtr targetendp = IntPtr.Add(strstartp, targetend * sizeof(char));

                StringBuilder msgSb = new StringBuilder(80); // エラーメッセージ格納用領域を確保
                int ret = BoSubst(pattern, subst, options, strstartp, targetstartp, targetendp, null, ref rxp, msgSb);
                if (ret < 0)
                {
                    // エラーあり
                    throw new ArgumentException(msgSb.ToString());
                }
                if (ret > 0)
                {
                    // 置換成功：構造体ポインタの最新の内容を取り出し
                    BREGEXP rx = (BREGEXP)(Marshal.PtrToStructure(rxp, typeof(BREGEXP)));
                    if (match != null)
                    {
                        Group replacedMatch = rx.CreateGroup(0, this); // 置換された部分のマッチ情報を取得
                        if (match.Index != replacedMatch.Index || match.Length != replacedMatch.Length)
                        {
                            // 検索時／置換時でマッチ箇所が同一にならなかった（通常発生しない。発生したらBregonigRegexのバグ、要究明）
                            throw new SystemException("置換対象のマッチ箇所(Index=" + match.Index + ", Match=" + match.Length + ")"
                                + "とは異なる箇所(Index=" + replacedMatch.Index + ", Match=" + replacedMatch.Length + ")が置換されました。");
                        }
                        if (rx.outp == null)
                        {
                            return string.Empty; // 置換結果が空文字となった
                        }
                        // 置換部分の開始位置＝置換結果全体の開始位置＋（マッチ箇所の開始位置、ただし探索開始位置考慮）
                        IntPtr replacedstart = IntPtr.Add(rx.outp, (match.Index - targetstart) * sizeof(char));
                        // 置換部分の置換後文字列長＝マッチ箇所の置換前文字列長＋置換結果全体の文字列長－置換前文字列全体の文字列長
                        int len = match.Length
                            + (int)((rx.outendp.ToInt64() - rx.outp.ToInt64()) / sizeof(char))
                            - (targetend - targetstart);
                        // 置換部分の文字列内容のみをピンポイントで抜き出す
                        return Marshal.PtrToStringUni(replacedstart, len);
                    }
                    else
                    {
                        // 置換後文字列全体を組み立て
                        StringBuilder sb = new StringBuilder();
                        if (targetstart > 0) { sb.Append(input.Substring(0, targetstart)); }
                        if (rx.outp != null)
                        {   // 空文字列に置換されていなければ、置換結果の文字列を取り出し
                            int len = (int)((rx.outendp.ToInt64() - rx.outp.ToInt64()) / sizeof(char));
                            sb.Append(Marshal.PtrToStringUni(rx.outp, len));
                        }
                        if (targetend < input.Length) { sb.Append(input.Substring(targetend)); }
                        return sb.ToString();
                    }
                }
                else
                {
                    // 置換箇所なし
                    if (match != null)
                    {
                        // 検索時／置換時でマッチ箇所が同一にならなかった（通常発生しない。発生したらBregonigRegexのバグ、要究明）
                        throw new SystemException("置換対象のマッチ箇所(Index=" + match.Index + ", Match=" + match.Length + ")が検出できませんでした。");
                    }
                    // 置換前テキスト内容のまま変更なし
                    return input;
                }
            }
        }

        /// <summary>
        /// bregonig.dllを用いてパターンマッチ処理を実行します。
        /// </summary>
        /// <param name="pattern">検索パターン(NULLを指定すると、前回のコンパイル済みパターンを使用する)</param>
        /// <param name="options">オプション</param>
        /// <param name="strstartp">検索対象文字列</param>
        /// <param name="targetstartp">検索開始位置</param>
        /// <param name="targetendp">検索終了モード</param>
        /// <param name="one_shot">ワンショットモード：通常はfalseを指定</param>
        /// <param name="rxp">BREGEXP 構造体ポインタ (rx) へのポインタ。初回はIntPtr(0)を指定して呼び出す。</param>
        /// <param name="msg">エラーメッセージの格納先。NULL不可、80文字分の領域が必要</param>
        /// <returns>正:マッチ箇所あり、0:マッチ箇所なし、負:エラー</returns>
        [DllImport("bregonig.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        protected static extern int BoMatch(
            string pattern,
            string options,
            IntPtr strstartp,
            IntPtr targetstartp,
            IntPtr targetendp,
            bool one_shot,
            ref IntPtr rxp,
            StringBuilder msg);

        /// <summary>
        /// bregonig.dllを用いて置換処理を実行します。
        /// </summary>
        /// <param name="pattern">検索パターン(NULLを指定すると、前回のコンパイル済みパターンを使用する)</param>
        /// <param name="subst">置換パターン(NULLを指定すると、前回のコンパイル済みパターンを使用する)</param>
        /// <param name="options">オプション</param>
        /// <param name="strstartp">検索対象文字列</param>
        /// <param name="targetstartp">検索開始位置</param>
        /// <param name="targetendp">検索終了モード</param>
        /// <param name="one_shot">コールバック関数へのポインタ：通常はnullを指定</param>
        /// <param name="rxp">BREGEXP 構造体ポインタ (rx) へのポインタ。初回はIntPtr(0)を指定して呼び出す。</param>
        /// <param name="msg">エラーメッセージの格納先。NULL不可、80文字分の領域が必要</param>
        /// <returns>正:マッチ箇所あり、0:マッチ箇所なし、負:エラー</returns>
        [DllImport("bregonig.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        protected static extern int BoSubst(
            string pattern,
            string subst,
            string options,
            IntPtr strstartp,
            IntPtr targetstartp,
            IntPtr targetendp,
            [MarshalAs(UnmanagedType.FunctionPtr)] BoSubstCallbackDelegate callback,
            ref IntPtr rxp,
            StringBuilder msg);

        /// <summary>置換コールバック関数の定義</summary>
        /// <param name="kind">0 固定</param>
        /// <param name="value">置換回数</param>
        /// <param name="index">ptrdiff_t 置換位置</param>
        /// <returns>true:処理継続、false:処理中断</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        protected delegate bool BoSubstCallbackDelegate(int kind, int value, IntPtr index);

        /// <summary>
        /// BREGEXPのコンパイルブロックを解放します。
        /// </summary>
        /// <param name="rx">BREGEXP 構造体ポインタ</param>
        [DllImport("bregonig.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        protected static extern void BRegfree(IntPtr rx);

        #endregion
    }
}
