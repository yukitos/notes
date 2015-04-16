# 独自のF# LINQクエリビルダーを作成する

> 原題：[Writing custom F# LINQ query builder][link1]

私が開催している [F# in Finance][link2] コースの参加者である[Stuart][link3] から、
F#で独自のクエリを作成するにはどうしたらいいかという、
少し難しい質問を最近受けました。
というのも、彼はAmazon DynamoDB用のいい感じなクエリライブラリを
作成しようと思っているとのことでした(彼の[プロジェクトはこちら][link4])。

そういえば確かにクエリビルダーを作成する際に参考となるような、
よいリソースを知らないということに気がついたので、
それならばこうして記事を書く事にしたというわけです。
アイディアとしては以下のようなコードを書けるようにしたいということです：

```fsharp
query {
    for p in DynamoDB.People do
    where (p.Age > 10)
    where (p.Name = "Tomas")
    select (p.Age, p.Name) }
```

`DynamoDB` は(Dynamo DB上で利用可能なすべてのテーブルを利用できるように)
型プロバイダーで生成された型とすることもできるでしょう
上の例では組み込みの`query`ビルダーを使っていて、
これを拡張していくことも可能ではありますが、
しかしその場合、私が知る限りではLINQの式ツリーを使わないといけないはずです。
そこで今回の記事では別の方法として、独自のビルダーを作成していくことにします
(つまり`query { ... }`ではなく`dynamo { ... }`というコードになります)。



[link1]: http://tomasp.net/blog/2015/query-translation/index.html
[link2]: http://www.fsharpworks.com/workshops/finance.html
[link3]: https://twitter.com/stuart_j_davies
[link4]: https://github.com/stuartjdavies/FSharp.Cloud.AWS/blob/master/FSharp.Cloud.AWS/DynamoDB.fs
