# 鉄道指向プログラミング #
関数型アプリケーションのためのレシピ パート2
原文：[Railway oriented programming][link01]

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

前回はユースケースを処理単位に分解する方法と、発生したエラーを以下のようにエラー用の回路に逃がす必要があるという説明をしました：

![Combine to a single "failure" path][link02]

今回はこれらのステップ関数を様々な方法で1つのユニットとして組み立てる方法を紹介します。
関数の内部設計については別の記事で説明する予定です。

## 1つのステップを表す関数を設計する ##

まずはステップの詳細を確認していきましょう。
たとえば検証用の関数です。
これはどのように機能すべきでしょうか？
何かしらのデータを受け取って、何を出力すればいいのでしょうか？

おそらくは2つのケースが考えられます。
1つはデータが正常な場合(正常パス)、そしてもう1つは何か問題がある場合で、この場合には以下のように別の経路をたどるようにして、残る処理がスキップされるようにします：

![Validate function][link03]

しかし以前と同様に、この関数は適切な関数にはなり得ません。
関数は1つの出力しか行えないため、前回定義した``Result``を使うことになります。

```fsharp
type Result<'TSuccess, 'TFailure> =
    | Success of 'TSuccess
    | Failure of 'TFailure
```

そうするとダイアグラムも以下のようになります：

![Validate function with one output][link04]

これが実際にどのように動作するのかは、以下のような具体的な検証用関数の例で確認できるでしょう：

```fsharp
type Request = { name:string; email:string }

let validateInput input =
    if input.name = "" then Failure "名前を入力してください"
    else if input.email = "" then Failure "メールアドレスを入力してください"
    else Success input // 成功パス
```

この関数の型を確認すると、コンパイラは``Request``を受け取って成功時には``Request``を、失敗時には``string``をデータに持つような``Result``を返すものだと判断したことがわかります：

```fsharp
validateInput : Request -> Result<Request,string>
```

別の関数に対しても同じ方法で分析することができます。
いずれの関数も同じ「形」をしていることが確認できます。
つまり何らかの入力を受け取り、成功/失敗を出力するという形になっているはずです。

> 予防線：関数は2つの出力を持つことができないと説明したばかりですが、上のような関数を指して「2つの出力を行う」関数と呼んでしまうことがあるかもしれません。
> 当然ながらそれは2つのケースを出力にする関数のことを指しているつもりです。

## 鉄道指向プログラミング ##

さて、そうするとたくさんの「1入力 -> 成功/失敗を出力」という関数が揃いましたが、どうやってこれらを連結すればよいのでしょうか？

やりたいことは``Success``の出力を次の入力として渡し、ただし``Failure``出力の場合には次の関数をスキップするようにしたいということです。
この概念を表すダイアグラムは以下のようになります：

![Connect success output, or bypass next function][link05]

この状況を例えるには皆さんがおそらく見慣れている、うってつけのものがあります。
鉄道です！

鉄道にはスイッチ(イギリスではポイント)があり、電車の進路を別の路線へと切り替えることができるようになっています。
つまり「成功/失敗」関数を以下のような鉄道のスイッチと見なすことができるわけです：

!["Success/Failure" functions as railway switch][link06]

そして横につなげてみることができます。

![Two railways in a row][link07]

2つの失敗路線を連結するにはどうすればよいでしょうか？
もちろんこういう感じです！

![Connect two failure tracks][link08]

たくさんのスイッチがある場合でも、下の図のようにすればどこまでも2路線のシステムを延長できます：

![Two track system][link09]

上側の路線が成功パスで、下側が失敗パスです。



[link01]: http://fsharpforfunandprofit.com/posts/recipe-part2/ "Railway oriented programming"
[link02]: img/01-10.png "Figure 01-10.png"
[link03]: img/02-01.png "Figure 02-01.png"
[link04]: img/02-02.png "Figure 02-02.png"
[link05]: img/02-03.png "Figure 02-03.png"
[link06]: img/02-04.png "Figure 02-04.png"
[link07]: img/02-05.png "Figure 02-05.png"
[link08]: img/02-06.png "Figure 02-06.png"
[link09]: img/02-07.png "Figure 02-07.png"
