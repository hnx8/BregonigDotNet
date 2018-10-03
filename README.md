# BregonigDotNet
This is the source code to make bregonig.dll "Onigmo" regular expression available from C#.  
C#からbregonig.dllによる鬼雲の正規表現を利用可能にするためのソースコードです。  

.NET Framework標準の正規表現（System.Text.RegularExpressions）では提供されていない高度な正規表現が利用できるようになります。


## VisualStudioのC#プロジェクトへの導入手順
bregonig.dllの公式サイト http://k-takata.o.oo7.jp/mysoft/bregonig.html より、
最新バージョン（もしくはVer.3以降の任意のバージョン）のbregonigを入手してください。   

[BregonigRegex.cs](./BregonigRegex.cs)をC#のプロジェクトに追加してください。  
またbregonig.dllをプロジェクトに含め、出力ディレクトリにコピー「する」設定にしてください。
（あるいは、binフォルダに直接bregonig.dllを配置してください）


## 使い方
[sample/Program.cs](./sample/Program.cs)を参照ください。  
基本的には.NET Framework標準のRegexクラスと同じようなメソッド/プロパティで操作できます。  
ただしインスタンス化して使用する際にはDisposeする必要があります。usingステートメントを使うのが無難です。

利用可能な正規表現／オプション指定はbregonig.dllの仕様通りです。  
（Unicode版のBoMatch()関数を呼び出すことで正規表現を実行しています）  
詳細は、  
 http://k-takata.o.oo7.jp/mysoft/bregonig.html#SCT-5.1.  
 http://k-takata.o.oo7.jp/mysoft/bregonig.html#SCT-4.3.1-OPTIONS  
を参照ください。


## ライセンス
BregonigRegex.cs は [MIT License](https://github.com/hnx8/BregonigDotNet/blob/master/LICENSE)です。  
※bregonig.dllの配布にあたっては、bregonig.dllのライセンス http://k-takata.o.oo7.jp/mysoft/bregonig.html#SCT-9. / https://github.com/k-takata/bregonig#license をご確認ください。


## 組み込み使用例
私(hnx8)が作成・公開している[TresGrep](http://hp.vector.co.jp/authors/VA055804/TresGrep/)というgrepツールにもこのソースコードと同等の実装を組み込んでいます。  
.NETFramework標準／鬼雲の正規表現を切り替えて検索することができます。挙動・パフォーマンスの比較にも使える・・・かもしれません。


## 謝辞
すばらしい正規表現エンジン/ライブラリを公開してくださっているOnigmo/bregonig.dll作者の k-takata 様（ http://k-takata.o.oo7.jp/ https://github.com/k-takata/ ）に感謝いたします。
