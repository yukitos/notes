# F# 3.1 言語仕様(ワーキングドラフト)

> 注釈：この文章はMicrosoftResearchおよびMicrosoftDeveloperDivisionによって
> 2013年6月に作成された、F# 3.1リリース向けの言語仕様です。

この言語仕様には3.1の実装と一致しない箇所がある場合があります。
文章中ではそれらの該当箇所にコメントの形でなるべく注釈を加えてあります。
言及のない不一致を見つけた場合は是非連絡してください。
そうすれば将来リリースされる言語仕様ではそれとわかるようにさせていただきます。
F#の開発チームはいつでもこの言語仕様、およびF#の設計や実装に対する
フィードバックを歓迎します。
フィードバックを送信するには、
http://github.com/fsharp/fsfoundation/docs/language-spec
にIssueをオープンしたり、コメントを追加したり、
Pull Requestを送信したりといった方法があります。

言語仕様の最新版は [fsharp.org](http://fsharp.org/) にあります。
これまでのドキュメントに対するF# ユーザーコミュニティからのフィードバックには
大変感謝しています。

この仕様書の一部ではC# 4.0やUnicode、IEEEといった仕様への言及があります。

**著者：**
Don Syme および補佐として Anar Alimov, Keith Battocchi, Jomo Fisher,
Michael Hale, Jack Hu, Luke Hoban, Tao Liu, Dmitry Lomov,
James Margetson, Brian McNamara, Joe Pamer, Penny Orwick,
Daniel Quirk, Kevin Ransom, Chris Smith, Matteo Taveggia,
Donna Malayeri, Wonseok Chae, Uladzimir Matsveyeu, Lincoln Atkinson
等による。

**警告**

_© 2005-2013 Microsoft Corporation and contributors. [Apache 2.0 License](http://www.apache.org/licenses/LICENSE-2.0.html)ライセンスの下に利用出来ます。_

_Microsoft, Windows, Visual F#はアメリカ合衆国 Microsoft Corporationの商標、あるいはアメリカ合衆国内外における登録商標です。_

_文章内で言及されるその他の製品や会社名にはそれぞれ固有の所有者が存在する場合があります。_

**更新履歴**

* 2014年 5月 バージョン番号をF# 3.1に変更
* 2013年 6月 F# 3.1用の最初の更新 ([言語のアップデートに関するオンラインの議論](http://blogs.msdn.com/b/fsharpteam/archive/2013/06/27/announcing-a-pre-release-of-f-3-1-and-the-visual-f-tools-in-visual-studio-2013.aspx)を参照)
* 2012年 9月 F# 3.0用の更新
* 2012年 8月 書式の更新
* 2011年12月 文法の要約を更新
* 2011年 2月 用語集と索引の更新、書式の修正
* 2010年 8月 用語集と索引の更新、書式の修正

## 目次

