using System;
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
        //   2018.10.01 written by hnx8(H.Takahashi)
        //   https://github.com/hnx8/BregonigDotNet
        //
        // C#からbregonig.dllの鬼雲による正規表現を利用するためのクラスです。
        // http://k-takata.o.oo7.jp/mysoft/bregonig.html にて配布されている
        // bregonig.dll(Ver.3以降)を実行フォルダに配置して下さい。
        //
        // 利用可能な正規表現／オプション指定は、
        //  http://k-takata.o.oo7.jp/mysoft/bregonig.html#SCT-5.1.
        //  http://k-takata.o.oo7.jp/mysoft/bregonig.html#SCT-5.2.
        // を参照してください。
        //
        ///////////////////////////////////////////////////////////////////////

        #region properties ----------------------------------------------------
        /// <summary>
        /// 正規表現にマッチした部分文字列の開始位置を返します。
        /// </summary>
        /// <remarks>マッチしていない場合は-1を返します。</remarks>
        public int Index
        {
            get { return (rx == null) ? -1 : GetIndex(0); }
        }

        /// <summary>
        /// 正規表現にマッチした部分文字列の長さを返します。
        /// </summary>
        /// <remarks>マッチしていない場合は0を返します。</remarks>
        public int Length
        {
            get { return (rx == null) ? 0 : GetLength(0); }
        }

        /// <summary>
        /// 正規表現にマッチした部分文字列を返します。
        /// </summary>
        /// <remarks>マッチしていない場合はnullを返します。</remarks>
        public string Value
        {
            get { return (rx == null) ? null : text.Substring(GetIndex(0), GetLength(0)); }
        }

        /// <summary>
        /// 正規表現に一致したグループを表す構造体です。
        /// </summary>
        public struct Group
        {
            private readonly string text; // 入力文字列

            /// <summary>キャプチャした部分文字列の開始位置を返します。キャプチャされていない場合は-1となります。</summary>
            public readonly int Index;

            /// <summary>キャプチャした部分文字列の長さを返します。キャプチャされていない場合は0となります。</summary>
            public readonly int Length;

            /// <summary>キャプチャした部分文字列を返します。キャプチャされていない場合はnullとなります。</summary>
            public string Value
            {
                get { return (Index < 0) ? null : text.Substring(Index, Length); }
            }

            internal Group(string text, int index, int length)
            {
                this.text = text;
                Index = index;
                Length = length;
            }
        }
        /// <summary>
        /// 正規表現マッチ箇所についてのキャプチャされたグループの配列を返します。
        /// [0]=マッチ箇所全体、[1]～[n]=グループ($1-$n)です。
        /// </summary>
        /// <remarks>
        /// マッチしていない場合はnullを返します。
        /// </remarks>
        public Group[] Groups
        {
            get
            {
                if (rx == null) { return null; }

                int nparens = rx.Value.GetNparens();
                Group[] ret = new Group[nparens + 1];
                for (int i = 0; i <= nparens; i++)
                {
                    bool success = (rx.Value.GetStartp(i) != IntPtr.Zero);
                    ret[i] = ((success)
                        ? new Group(text, GetIndex(i), GetLength(i))
                        : new Group(null, -1, 0));
                }
                return ret;
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
        public BregonigRegex(string pattern, string options = "")
        {
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            this.rxp = IntPtr.Zero;
            IntPtr textp = Marshal.StringToHGlobalUni("");
            try
            {
                BoMatch(pattern, options, textp, textp, textp, true); // 正規表現を初期化
            }
            finally
            {
                Marshal.FreeHGlobal(textp);
            }
        }

        /// <summary>
        /// 鬼雲正規表現クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="pattern">正規表現パターン文字列</param>
        /// <param name="options">正規表現オプション（RegexOptionsで指定）</param>
        /// <remarks>
        /// アンマネージドリソースを使用します。使用後はDispose()してください。
        /// 正規表現にエラーがある場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
        /// bregonig.dllの読み込みに失敗した場合は、DllNotFoundException(dllファイルがない)/TypeLoadException(BoMatchメソッドがない)/BadImageFormatException(x86/x64不一致)などの例外が投げられます。
        /// </remarks>
        public BregonigRegex(string pattern, RegexOptions options)
            : this(pattern,
                "u" // .NETFrameworkのRegexOptionsとおおむね同等の挙動となるようオプションを指定する
                + (options.HasFlag(RegexOptions.IgnoreCase) ? "i" : "")
                + (options.HasFlag(RegexOptions.Multiline) ? "m" : "")
                + (options.HasFlag(RegexOptions.Singleline) ? "s" : "")
                + (options.HasFlag(RegexOptions.IgnorePatternWhitespace) ? "x" : "")
            ) { }

        #endregion

        #region methods -------------------------------------------------------
        /// <summary>
        /// 指定した入力文字列内で、正規表現パターンと一致する箇所を検索します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <returns>true:見つかった、false:見つからなかった</returns>
        public bool Match(string input)
        {
            return Match(input, 0, input.Length);
        }

        /// <summary>
        /// 入力文字列の指定された開始位置・文字数の範囲で、正規表現パターンと一致する箇所を検索します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="beginning">開始位置</param>
        /// <param name="length">文字数</param>
        /// <returns>true:見つかった、false:見つからなかった</returns>
        /// <remarks>
        /// 入力文字列・開始位置・文字数が妥当ではない場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
        /// </remarks>
        public bool Match(string input, int beginning, int length)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (beginning < 0 && beginning > text.Length)
            {
                throw new ArgumentOutOfRangeException("beginning", beginning, "");
            }
            if (length < 0 && beginning + length > text.Length)
            {
                throw new ArgumentOutOfRangeException("length", length, "");
            }

            if (!String.ReferenceEquals(input, this.text))
            {
                // 検索対象の入力文字列が変わった場合のみアンマネージドメモリへ割り当て（変わっていないなら割り当てなおさない）
                freeText();
                strstartp = Marshal.StringToHGlobalUni(input);
                this.text = input;
            }
            IntPtr targetstartp = IntPtr.Add(strstartp, beginning * sizeof(char));
            targetendp = IntPtr.Add(targetstartp, length * sizeof(char));
            return BoMatch(null, null, strstartp, targetstartp, targetendp, false);
        }

        /// <summary>
        /// </summary>
        /// 一致する対象が最後に見つかった位置の終了位置から開始して、次に一致する対象を検索します。
        /// <returns>true:見つかった、false:見つからなかった</returns>
        public bool NextMatch()
        {
            if (rx == null) { return false; }
            IntPtr nextTarget = rx.Value.GetEndp(0);
            if (nextTarget.ToInt64() == rx.Value.GetStartp(0).ToInt64())
            {
                // 直前のマッチが長さ0だった場合は次の位置(無限ループ回避)
                nextTarget = IntPtr.Add(nextTarget, sizeof(char));
            }
            return BoMatch(null, null, strstartp, nextTarget, targetendp, false);
        }

        #endregion

        #region disposing -----------------------------------------------------
        private void freeText()
        {
            if (this.strstartp != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(strstartp);
                this.strstartp = IntPtr.Zero;
            }
        }

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
            freeText();
            if (rxp != IntPtr.Zero)
            {
                BRegfree(rxp);
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

        #region private -------------------------------------------------------
        private IntPtr rxp;         // BREGEXP 構造体へのポインタ（要BRegFree解放）
        private BREGEXP? rx = null; // BREGEXP 構造体（マッチ箇所なしの場合null）
        private string text;        // 入力文字列
        private IntPtr strstartp;   // 入力文字列をアンマネージメモリに割り当てた先のポインタ（要Marshal解放）
        private IntPtr targetendp;  // 直前の検索マッチ終了位置

        /// <summary>
        /// bregexp(bregonig)が内部で使用するコンパイルブロック構造体
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BREGEXP
        {
            IntPtr outp;      // (result string start ptr)
            IntPtr outendp;   // (result string end ptr)
            int splitctr;     // (split result counter)
            IntPtr splitpp;   // (split result pointer ptr)
            int rsv1;         // (reserved for external use)
            IntPtr parap;     // (parameter start ptr ie. "s/xxxxx/yy/gi")
            IntPtr paraendp;  // (parameter end ptr)
            IntPtr transtblp; // (translate table ptr)
            IntPtr startpp;   // マッチしたデータの先頭ポインタ（の配列。[0]=マッチ箇所全体の開始位置、[1-n]=グループ($1-$n)の開始位置）
            IntPtr endpp;     // マッチしたデータの最終ポインタ+1（の配列。[0]=マッチ箇所全体の終了位置、[1-n]=グループ($1-$n)の終了位置）
            int nparens;      // パターンの中の() の数。 $1,$2,...,$n を調べるときに使用

            /// <summary>
            /// マッチ範囲の開始位置（ポインタ）を返します。
            /// </summary>
            /// <param name="index">0=マッチ箇所全体の開始位置、1-n=グループ($1-$n)の開始位置</param>
            /// <returns>開始位置ポインタ</returns>
            internal IntPtr GetStartp(int index)
            {
                return Marshal.ReadIntPtr(startpp, index * IntPtr.Size);
            }
            /// <summary>
            /// マッチ範囲の終了位置（ポインタ）を返します。
            /// </summary>
            /// <param name="index">0=マッチ箇所全体の終了位置、1-n=グループ($1-$n)の終了位置</param>
            /// <returns>終了位置ポインタ（最終文字＋１の位置）</returns>
            internal IntPtr GetEndp(int index)
            {
                return Marshal.ReadIntPtr(endpp, index * IntPtr.Size);
            }
            /// <summary>
            /// マッチ範囲のグループの数を返します。
            /// </summary>
            /// <returns>パターンの中の() の数。 $1,$2,...,$n を調べるときに使用</returns>
            internal int GetNparens()
            {
                return nparens;
            }
        }

        /// <summary>
        /// キャプチャした部分文字列の開始位置を返します。
        /// </summary>
        /// <param name="index">0=マッチ箇所全体、1-n=グループ($1-$n)</param>
        /// <returns>文字列開始位置</returns>
        private int GetIndex(int index)
        {
            Int64 val = rx.Value.GetStartp(index).ToInt64() - strstartp.ToInt64();
            return (int)(val / sizeof(char));
        }

        /// <summary>
        /// キャプチャした部分文字列の長さをを返します。
        /// </summary>
        /// <param name="index">0=マッチ箇所全体、1-n=グループ($1-$n)</param>
        /// <returns>文字列長（char1文字=1）</returns>
        private int GetLength(int index)
        {
            Int64 val = rx.Value.GetEndp(index).ToInt64() - rx.Value.GetStartp(index).ToInt64();
            return (int)(val / sizeof(char));
        }

        /// <summary>
        /// BoMatch()呼び出し/結果処理簡略化
        /// </summary>
        /// <param name="pattern">検索パターン(NULLを指定すると、前回のコンパイル済みパターンを使用する)</param>
        /// <param name="options">オプション</param>
        /// <param name="strstartp">検索対象文字列</param>
        /// <param name="targetstartp">検索開始位置</param>
        /// <param name="targetendp">検索終了モード</param>
        /// <param name="one_shot">ワンショットモード：通常はfalseを指定</param>
        /// <returns>true:マッチ箇所あり、false:マッチ箇所なし</returns>
        private bool BoMatch(
            string pattern,
            string options,
            IntPtr strstartp,
            IntPtr targetstartp,
            IntPtr targetendp,
            bool one_shot)
        {
            StringBuilder msgSb = new StringBuilder(80); // エラーメッセージ格納用領域を確保
            int ret = BoMatch(pattern, options, strstartp, targetstartp, targetendp, one_shot, ref rxp, msgSb);
            if (ret < 0)
            {   // エラーあり
                throw new ArgumentException(msgSb.ToString());
            }
            //
            if (ret > 0)
            {
                // マーシャリングで構造体ポインタの最新の内容を取り出し
                this.rx = (BREGEXP)(Marshal.PtrToStructure(rxp, typeof(BREGEXP)));
                return true;
            }
            else
            {
                this.rx = null;
                return false;
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
        private static extern int BoMatch(
            string pattern,
            string options,
            IntPtr strstartp,
            IntPtr targetstartp,
            IntPtr targetendp,
            bool one_shot,
            ref IntPtr rxp,
            StringBuilder msg);

        /// <summary>
        /// BREGEXPのコンパイルブロックを解放します。
        /// </summary>
        /// <param name="rx">BREGEXP 構造体ポインタ</param>
        [DllImport("bregonig.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern void BRegfree(IntPtr rx);

        #endregion
    }
}
