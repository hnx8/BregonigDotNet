﻿using System;
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
        //   2018.10.01 written by hnx8(H.Takahashi)
        //   https://github.com/hnx8/BregonigDotNet
        //
        // C#からbregonig.dllの鬼雲による正規表現を利用するためのクラスです。
        // http://k-takata.o.oo7.jp/mysoft/bregonig.html にて配布されている
        // bregonig.dll(Ver.3以降)を実行フォルダに配置して下さい。
        //
        // 利用可能な正規表現／オプション指定は、
        //  http://k-takata.o.oo7.jp/mysoft/bregonig.html#SCT-5.1.
        //  http://k-takata.o.oo7.jp/mysoft/bregonig.html#SCT-4.3.1-OPTIONS
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
            // これらの項目はNextMatch()/Result()で使用する。

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
                return regex.BoMatch(null, null, input, nextBeginning, targetend, false);
            }

            /// <summary>
            /// 正規表現にマッチした部分文字列を、指定した置換パターンを展開して返します。
            /// </summary>
            /// <param name="replacement">使用する置換パターン</param>
            /// <returns>置換パターン適用後の文字列</returns>
            /// <remarks>
            /// BregonigRegex正規表現オブジェクトがすでにDispose済の場合は、ObjectDisposedExceptionが投げられます。
            /// 置換パターンにエラーがある場合は、ArgumentExceptionないしそのサブクラスの例外が投げられます。
            /// </remarks>

            //TODO 後日実装予定
            //public string Result(string replacement)
            //{
            //    throw new NotImplementedException();
            //}
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

            private readonly string input; // grep実行時に指定されていた検索テキスト

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
        public static IEnumerable<Matched> Matches(string input, string pattern, string options = "")
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
        public static bool IsMatch(string input, string pattern, string options = "")
        {
            using (BregonigRegex regex = new BregonigRegex(pattern, options))
            {
                return regex.IsMatch(input);
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

            //TODO 置換処理の都合でオプション「g」は取り込まないようにする必要あり？
            this.pattern = pattern;
            this.options = options;
            this.rxp = IntPtr.Zero;
            try
            {
                BoMatch(pattern, options, "", 0, 0, true); // 正規表現を初期化
            }
            finally
            {
                freeText(); // コンストラクタで例外となった場合もリソース解放は確実に実施する
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
            : this(pattern, ConvertOption(options)) { }

        /// <summary>
        /// RegexOptionsで指定された正規表現オプションを鬼雲正規表現オプション文字列へ変換します。
        /// </summary>
        /// <param name="options">正規表現オプション（文字列で指定）</param>
        /// <returns>鬼雲正規表現オプション文字列</returns>
        private static string ConvertOption(RegexOptions options)
        {
            StringBuilder sb = new StringBuilder('u');
            if (options.HasFlag(RegexOptions.IgnoreCase)) { sb.Append('i'); }
            if (options.HasFlag(RegexOptions.Multiline)) { sb.Append('m'); }
            if (options.HasFlag(RegexOptions.Singleline)) { sb.Append('s'); }
            if (options.HasFlag(RegexOptions.IgnorePatternWhitespace)) { sb.Append('x'); }
            return sb.ToString();
        }
        #endregion

        #region methods -------------------------------------------------------
        /// <summary>コンストラクタで指定された正規表現パターン文字列を返します。</summary>
        public override string ToString()
        {
            return pattern;
        }

        /// <summary>
        /// 指定した入力文字列内で、正規表現パターンと一致する箇所を検索します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <returns>検索によって見つかった Matched オブジェクト。見つからなかった場合はnull</returns>
        public Matched Match(string input)
        {
            return Match(input, 0, input.Length);
        }

        /// <summary>
        /// 入力文字列の指定された開始位置・文字数の範囲で、正規表現パターンと一致する箇所を検索します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="beginning">開始位置</param>
        /// <returns>検索によって見つかった Matched オブジェクト。見つからなかった場合はnull</returns>
        public Matched Match(string input, int beginning)
        {
            return Match(input, beginning, input.Length - beginning);
        }

        /// <summary>
        /// 入力文字列の指定された開始位置・文字数の範囲で、正規表現パターンと一致する箇所を検索します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="beginning">開始位置</param>
        /// <param name="length">文字数</param>
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
        /// <returns>検索によって見つかった Matched オブジェクトの列挙</returns>
        public IEnumerable<Matched> Matches(string input)
        {
            return Matches(input, 0);
        }

        /// <summary>
        /// 指定した入力文字列内で、正規表現パターンと一致する箇所を検索しすべて列挙します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="beginning">開始位置</param>
        /// <returns>検索によって見つかった Matched オブジェクトの列挙</returns>
        public IEnumerable<Matched> Matches(string input, int beginning)
        {
            Matched match = Match(input, beginning);
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
        /// <returns>true:見つかった、false:見つからなかった</returns>
        public bool IsMatch(string input)
        {
            return IsMatch(input, 0);
        }

        /// <summary>
        /// 指定した入力文字列内で、正規表現パターンと一致する箇所が見つかるかどうかを返します。
        /// </summary>
        /// <param name="input">検索対象とする入力文字列</param>
        /// <param name="beginning">開始位置</param>
        /// <returns>true:見つかった、false:見つからなかった</returns>
        public bool IsMatch(string input, int beginning)
        {
            return (Match(input, beginning) != null);
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
        /// <summary>コンストラクタで指定された正規表現パターン文字列</summary>
        public readonly string pattern;
        /// <summary>コンストラクタで指定された正規表現オプション</summary>
        public readonly string options;
        /// <summary>BREGEXP 構造体へのポインタ（要BRegFree解放）</summary>
        private IntPtr rxp;
        /// <summary>入力文字列</summary>
        private string input;
        /// <summary>入力文字列をアンマネージメモリに割り当てた先のポインタ（要Marshal解放）</summary>
        private IntPtr strstartp;

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
            internal IntPtr startpp;   // マッチしたデータの先頭ポインタ（の配列。[0]=マッチ箇所全体の開始位置、[1-n]=グループ($1-$n)の開始位置）
            internal IntPtr endpp;     // マッチしたデータの最終ポインタ+1（の配列。[0]=マッチ箇所全体の終了位置、[1-n]=グループ($1-$n)の終了位置）
            internal int nparens;      // パターンの中の() の数。 $1,$2,...,$n を調べるときに使用
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
        private Matched BoMatch(
            string pattern,
            string options,
            string input,
            int targetstart,
            int targetend,
            bool one_shot)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("BREGEXP構造体");
            }
            // 検索対象の入力文字列が変わった場合のみアンマネージドメモリへ割り当て（変わっていないなら割り当てなおさない）
            if (!string.ReferenceEquals(input, this.input))
            {
                freeText();
                strstartp = Marshal.StringToHGlobalUni(input);
                this.input = input;
            }
            // 検索位置をポインタに変換
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
                // マッチあり：マーシャリングで構造体ポインタの最新の内容を取り出し
                BREGEXP rx = (BREGEXP)(Marshal.PtrToStructure(rxp, typeof(BREGEXP)));
                int arrayLength = rx.nparens + 1; // グループ数＋[0]マッチ箇所全体 の分
                Group[] groups = new Group[arrayLength];
                for (int i = 0; i < arrayLength; i++)
                {
                    IntPtr startp = Marshal.ReadIntPtr(rx.startpp, i * IntPtr.Size);
                    IntPtr endp = Marshal.ReadIntPtr(rx.endpp, i * IntPtr.Size);
                    Group g = null;
                    if (startp != IntPtr.Zero)
                    {
                        g = new Group(this.input,
                            (int)((startp.ToInt64() - strstartp.ToInt64()) / sizeof(char)),
                            (int)((endp.ToInt64() - startp.ToInt64()) / sizeof(char)));
                    }
                    groups[i] = g;
                }
                return new Matched(groups, this, targetstart, targetend);
            }
            else
            {
                return null;
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
