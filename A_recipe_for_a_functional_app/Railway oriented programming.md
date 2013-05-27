# 鉄道指向プログラミング #
関数型アプリケーションのためのレシピ パート2

原文：[Railway oriented programming][link01]

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

前回はユースケースを処理単位に分解する方法と、発生したエラーを以下のようにエラー用の回路に逃がす必要があるという説明をしました：

![「失敗」パスを1つにまとめる][link02]

今回はこれらのステップ関数を様々な方法で1つのユニットとして組み立てる方法を紹介します。
関数の内部設計については別の記事で説明する予定です。

## 1つのステップを表す関数を設計する ##

まずはステップの詳細を確認していきましょう。
たとえば検証用の関数です。
これはどのように機能すべきでしょうか？
何かしらのデータを受け取って、何を出力すればいいのでしょうか？

おそらくは2つのケースが考えられます。
1つはデータが正常な場合(正常パス)、そしてもう1つは何か問題がある場合で、この場合には以下のように別の経路をたどるようにして、残る処理がスキップされるようにします：

![検証用関数][link03]

しかし以前と同様に、この関数は適切な関数にはなり得ません。
関数は1つの出力しか行えないため、前回定義した``Result``を使うことになります。

```fsharp
type Result<'TSuccess, 'TFailure> =
    | Success of 'TSuccess
    | Failure of 'TFailure
```

そうするとダイアグラムも以下のようになります：

![1出力の検証用関数][link04]

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

![成否によって続く関数をスキップする][link05]

この状況を例えるには皆さんがおそらく見慣れている、うってつけのものがあります。
鉄道です！

鉄道にはスイッチ(イギリスではポイント)があり、電車の進路を別の路線へと切り替えることができるようになっています。
つまり「成功/失敗」関数を以下のような鉄道のスイッチと見なすことができるわけです：

![「成功/失敗」関数としてのスイッチ][link06]

そして横につなげてみることができます。

![横につなげた2つのスイッチ][link07]

2つの失敗路線を連結するにはどうすればよいでしょうか？
もちろんこういう感じです！

![失敗路線の連結][link08]

たくさんのスイッチがある場合でも、下の図のようにすればどこまでも2路線のシステムを延長できます：

![2路線システム][link09]

上側の路線が成功パスで、下側が失敗パスです。

ここで少し話を戻して全体像を見てみると、2車線の線路を覆い隠すようなブラックボックス関数がいくつかあり、それぞれの関数はデータを処理した後に次の関数へと結果を受け渡していることがわかります：

![2路線鉄道上にまたがった関数][link10]

しかし関数の中身を見ると、実際にはそれぞれの中にスイッチがあり、不正なデータであれば失敗用の路線に切り替えている形になっています：

![関数の中身][link11]

なお一度失敗用の路線に入ってしまうと(本来であれば)成功パスには決して戻さないという点に注意してください。
終点にたどり着くまでは以降の処理をスキップさせるだけです。

## 基本的な合成 ##

それぞれの関数を「連結する(glue)」前に、合成がどのように機能するのか簡単に説明しましょう。

標準的な関数を線路上に置かれたブラックボックス(あるいはトンネル)だとします。
ここには1つの入力と1つの出力があります。

1路線用の関数を複数接続したい場合、``>>``というシンボルで表される、左から右への合成演算子を使用します。

![左から右への合成演算子][link12]

この合成演算子は2路線用の関数にも適用できます：

![2路線関数に対する合成演算子][link13]

合成における唯一の制限は、左辺の関数における出力の型が右辺の関数における入力の型と一致しなければいけないということだけです。

今回の鉄道の例であれば、1路線出力を1路線入力、あるいは2路線出力を2路線入力に接続することができますが、2路線出力を1路線入力に接続することはできないということです。

![不正な合成][link14]

## スイッチを2路線用の入力に変換する ##

さてここで問題があります。

各ステップ用の関数はそのままだと1路線入力のスイッチになります。
しかし処理全体としては両方の路線を覆うような2路線システムになっていなければいけません。
つまり各関数は単に1路線入力(``Request``)ではなく、2路線入力(``Result``)できるようになっていなければいけないということです。

どうすれば2路線システムにスイッチを導入できるのでしょうか？

答えは単純です。
以下の図にあるように、各関数用の「穴」あるいは「スロット」を持ち、適切な2路線用の関数へと変換するような「アダプター」関数を用意すればよいのです：

![アダプター関数][link15]

また、実際のコードは以下のようになります。
このような処理は標準的には``bind``と呼ばれることが多いため、ここでもそれにならっています。

```fsharp
let bind switchFunction =
    fun twoTrackInput ->
        match twoTrackInput with
        | Success s -> switchFunction s
        | Failure f -> Failure f
```

bind関数はスイッチ関数を引数にとり、新しい1つの関数を返します。
新しい関数は2路線入力(``Result``型)を取り、各ケースをチェックします。
入力が``Success``であればその値を引数にして``switchFunction``を呼びます。
しかし入力が``Failure``の場合にはスイッチ関数がスキップされます。

このコードをコンパイルしてみると以下のようなシグネチャになっていることが確認できます：

```fsharp
val bind : ('a -> Result<'b,'c>) -> Result<'a,'c> -> Result<'b,'c>
```

このシグネチャは、``bind``関数が引数(``'a -> Result<..>``)を1つとり、2路線関数(``Result<..> -> Result<..>``)を出力とするというように解釈することもできます。

さらに具体的には以下の通りです：

* bindの引数(``switchFunction``)は任意の型``'a``をとり、``'b``(成功路線用)と``'c``(失敗路線用)を返します。
* リターンされた関数自体は、``'b``(成功用)と``'c``(失敗用)を型に持つ``Result``型の引数(``twoTrackInput``)をとります。
  型``'a``は``switchFunction``が期待する1路線の型と同じものです。
* リターンされた関数の出力は``'b``と``'c``という別の型を持った``Result``型です。
  この型はスイッチ関数の出力と同じ型になっています。

上記のように、この型シグネチャがまさに期待する通りのものになっていることが分かると思います。

なおこの関数は完全にジェネリックで、任意のスイッチ関数と任意の型に対応できます。
制限されるのは``switchFunction``の「形」だけで、具体的な型については特に制限されません。

### bind関数を別の方法で作成する ###

少し話がそれますが、bind関数は別の書き方をすることもできます。

1つは以下のように、内部で定義した``twoTrackInput``を2番目の引数として明示的に受け取るようにする方法です：

```fsharp
let bind switchFunction twoTrackInput =
    match twoTrackInput with
    | Success s -> switchFunction s
    | Failure f -> Failure f
```

これは最初の定義と全く同じものです。
2引数の関数が1引数の関数と全く同じだと言える理由が分からなければ、是非 [カリー化][link16] の記事を参照してください！

もう1つの書き方としては、以下のように``match..with``を``function``キーワードで書き換えてしまう方法です：

```fsharp
let bind switchFunction =
    function
    | Success s -> switchFunction s
    | Failure f -> Failure f
```

以上3つのコードスタイルを見かけることになると思いますが、引数が明確になっていたほうが非エキスパートであってもコードを読みやすいと思うため、筆者としては2番目のスタイル(``let bind switchFunction twoTrackInput =``)を推奨します。

## 例：複数の検証用関数を組み合わせる ##

コンセプトがうまくいっているかテストするために、ここで少しコードを書いてみましょう。

まずは既に定義済みのものがあります。
``Request``と``Result``、``bind``は以下の通りです：

```fsharp
type Result<'TSuccess, 'TFailure> =
    | Success of 'TSuccess
    | Failure of 'TFailure

type Request = {name:string; email:string}

let bind switchFunction twoTrackInput =
    match twoTrackInput with
    | Success s -> switchFunction s
    | Failure f -> Failure f
```

次に3つの関数を作成します。
それぞれは「スイッチ」関数で、最終的には1つの巨大な関数として組み合わせることになります：

```fsharp
let validate1 input =
    if input.name = "" then Failure "名前を入力してください"
    else Success input

let validate2 input =
    if input.name.Length > 50 then Failure "名前は50文字以下で入力してください"
    else Success input

let validate3 input =
    if input.email = "" then Failure "メールアドレスを入力してください"
    else Success input
```

次はこれらを連結できるように、それぞれの関数に対して``bind``関数を呼び出して2路線用の新しい関数を作成します。

その後、以下のように標準の関数合成演算子を使用して2路線関数を連結します：

```fsharp
/// 3つの検証用関数を1つにまとめます
let combinedValidation =
    // スイッチ関数を2路線関数に変換します
    let validate2' = bind validate2
    let validate3' = bind validate3
    // 2路線関数を連結します
    validate1 >> validate2' >> validate3'
```

``validate2'``と``validate3'``は2路線を入力とするような新しい関数です。
シグネチャを確認すると、``Result``を引数にとって``Result``をリターンするようになっていることがわかります。
しかし``validate1``は2路線入力できるように変換する必要はないという点に注意してください。
この入力は1路線のままになっていて、出力は既に2路線になっているため、合成に必要な条件を満たしています。

``validate1``(未bind)スイッチと``validate2``、``validate3``スイッチをそれぞれ``validate2'``、``validate3'``アダプターにして連結すると下図のようになります。

![検証用関数の連結][link16]

以下のように``bind``を「インライン化」することもできます：

```fsharp
let combinedValidation =
    // 2路線用関数を連結します
    validate1
    >> bind validate2
    >> bind validate3
```

不正な入力2パターンと正常入力1パターンをテストしてみましょう：

```fsharp
// テスト1
let input1 = {name=""; email=""}
combinedValidation input1
|> printfn "Result1=%A"

// ==> Result1=Failure "名前を入力してください"

// テスト2
let input2 = {name="Alice"; email=""}
combinedValidation input2
|> printfn "Result2=%A"

// ==> Result2=Failure "メールアドレスを入力してください"

let input3 = {name="Alice"; email="good"}
combinedValidation input3
|> printfn "Result3=%A"

// ==> Result3=Success {name = "Alice"; email = "good";}
```

是非上のコードを実際に試してみたり、違う値をテストしてみたりしてください。

> 上記3つの関数を直列ではなく並列に実行して、検証エラーを一度に取得できないだろうかと思うかもしれません。
> もちろん可能です。
> この記事で後ほど説明する予定です。

### パイプ化演算子としてのbind ###

``bind``関数の説明が続きますが、スイッチ関数をパイプ化するシンボルとしては``>>=``が一般的に使用されます。

定義は以下の通りで、左右に指定した関数を簡単に連結させることができるようになっています：

```fsharp
/// 中置演算子を作成します
let (>>=) twoTrackInput switchFunction =
    bind switchFunction twoTrackInput
```

> このシンボルは、合成演算子``>>``の後に線路のシンボル``=``を続けたものと覚えてください。

こういった演算子を使用すると、``>>=``演算子をスイッチ関数用のパイプ演算子(``|>``)とみなすことができます。

通常のパイプ演算では、左辺に1路線の値を指定して、右辺に通常の関数を指定します。
しかし「bindパイプ」演算子の場合、左辺には2路線の値を指定して右辺にスイッチ関数を指定します。

この演算子を使用すると、``combinedValidation``関数を以下のような方法でも作成できます。

```fsharp
let combinedValidation x =
    x
    |> validate1  // validate1は1路線入力なので通常のパイプ演算を使用しますが
                  // 結果としては2路線を出力するので...
    >>= validate2 // ...「bindパイプ」が使用できます。この結果も2路線出力なので
    >>= validate3 // ... さらに「bindパイプ」が使用できます。
```

前回の実装と異なる点は、今回の場合は関数指向というよりはデータ指向の実装になっているという点です。
初期のデータ値として``x``を明示的に受け取るようになっています。
``x``は最初の関数に渡されて、出力データが後続する関数に渡されていくという形になっています。

前回の実装では(以下に再掲しますが)データ引数を全くとらないものでした！
関数自体に焦点が置かれていて、関数毎のデータフローについては対象になっていません。

```fsharp
let combinedValidation =
    validate1
    >> bind validate2
    >> bind validate3
```

## bindを使用しない方法 ##

スイッチ関数を連結する別の方法としては、2路線入力関数に接続するのではなく、単純に直接それぞれのスイッチ関数を連結して、より大きなスイッチ関数を作成します。

つまり、以下のスイッチ関数があるとして：

![2つのスイッチ関数][link17]

以下のように連結します：

![連結後のスイッチ関数][link18]

しかしよく考えてみると、この連結した路線もまた違ったスイッチ関数だと見なすことができます！
中央のあたりを隠してみましょう。
そうすると1入力2出力になっていることがわかります：

![2つのスイッチ関数を合成すると新しいスイッチ関数のように見える][link19]

つまり実際には以下のようにしてスイッチ関数を連結できるというわけです：

![2つのスイッチ関数を連結する][link20]

それぞれの合成結果が別のスイッチになっているわけなので、さらに別のスイッチを追加してより大きな関数となり、やはりこれもスイッチなので別のスイッチを追加できるといった具合です。

スイッチを合成するコードは以下のようになります。
標準的なシンボルは``>=>``で、通常の合成用シンボルに似ていますが、間に線路が置かれた形になっています。

```fsharp
let (>=>) switch1 switch2 x =
    match switch1 x with
    | Success s -> switch2 s
    | Failure f -> Failure f
```

今回も実際の実装としては非常に単純です。
1路線入力``x``を最初のスイッチに渡します。
そして結果が成功であれば2番目のスイッチに結果を渡し、失敗した場合には2番目のスイッチが完全にスキップされます。

さてこれでbindを使用せずに``combinedValidation``関数が作成できるようになりました：

```fsharp
let combinedValidation =
    validate1
    >=> validate2
    >=> validate3
```

これが一番シンプルな形ではないかと思います。
もちろん拡張も非常に簡単で、4つめの検証用関数を追加したい場合には、最後の位置に追加するだけです。

### bind 対 スイッチ合成 ###

それぞれは一見すると同じに見えるものの、コンセプトがそれぞれ異なります。
何が異なるのでしょうか？

それぞれの機能は以下の通りです：

* **bind**は1つのスイッチ関数を引数にとります。
  スイッチ関数を完全な2路線(つまり2路線入力かつ2路線出力)関数に変換します。
* **スイッチ合成**は2つのスイッチ関数を引数にとります。
  一連のスイッチ関数を連結して、新しい1つのスイッチ関数を作成します。

スイッチ合成よりもbindを使用したほうがいい場合があるのでしょうか？
それはコンテキストによって異なります。
既に2路線システムが構築されていて、そこへさらにスイッチを追加する必要がある場合には、bindでスイッチ関数を2路線入力できるように変換する必要があります。

![既存の2路線システムがある場合にはbindが必要][link21]

一方、全体的なデータフローがスイッチの連鎖で構成されている場合にはスイッチ合成のほうが簡単でしょう。

![スイッチの合成][link22]

### bindの観点からのスイッチ合成 ###

偶然にも、スイッチ合成はbindを使用して記述することができます。
1つめのスイッチをbind後の2つめのスイッチと連結させればスイッチ合成と同じことができます。
つまり2つのスイッチがそれぞれあるとして：

![2つの独立したスイッチ][link23]

それぞれを合成してより大きなスイッチが作られます：

![合成語のスイッチ][link24]

これは2つめのスイッチに``bind``を使用した場合と同じです：

![2つめのスイッチにbindを使用する][link25]

この考え方でスイッチ合成を書き直すと以下のようになります：

```fsharp
let (>=>) switch1 switch2 =
    switch1 >> (bind switch2)
```

このスイッチ合成の実装は最初のものよりも単純で、それでいながらより抽象化されています。
しかしそれが初学者にとって簡単かどうかはまた別の問題です！
関数をデータの流れから考えるのではなく、正しく機能として認識することができるようになれば、このアプローチのほうが理解しやすいのではないかと思います。

## 単純な関数を鉄道指向プログラミングモデルへと変換する ##

いったんコツがつかめれば、ありとあらゆるものをこのモデルに適用できるようになります。

たとえばスイッチ関数ではなく、普通の関数を考えてみましょう。
また、それをフローの中に組み込みたいものとします。

具体的には検証が完了した後、メールアドレスから前後の空白を削除しつつ小文字に揃えたいとします。
コードとしては以下のような関数を用意します：

```fsharp
let canonicalizeEmail input =
    { input with email = input.email.Trim().ToLower() }
```

このコードは(1路線の)``Request``を受け取り、(1路線の)``Request``を返します。

これを検証処理と更新処理の間に挿入するにはどうしたらよいでしょうか？

この単純な関数をスイッチ関数に変換出来たとすれば、後は既に説明したようにスイッチ合成を行うだけです。

別の言い方をすれば、この関数用のアダプターブロックが必要だということです。
コンセプトとしては``bind``の場合と同じですが、今回の場合はアダプターブロックが1路線関数用のスロットを持ち、全体の「形」としてはアダプターブロックがスイッチになっていなければいけないという違いがあります。

![1路線関数用のスロットを持ったアダプターブロック][link26]

実装コードは単純です。
1路線関数の出力を2路線用の出力へと変換してやればよいだけです。
今回の場合、結果は常にSuccessになります。

```fsharp
// 通常の関数をスイッチに変換します
let switch f x =
    f x |> Success
```

鉄道用語で言えば、ある意味で廃線を増やしたとも言えるでしょう。
全体からすると(1路線入力、2路線出力の)スイッチ関数のように見えますが、当然ながら実際には失敗用の路線は単なるダミーで、決して使用されることがありません。

![失敗路線の増設][link27]

``switch``が出来上がれば、あとは``canonicalizeEmail``関数を最後の位置に連結させるだけです。
機能も増えてきたため、あわせて関数の名前を``usecase``に変更しましょう。

```fsharp
let usecase =
    validate1
    >=> validate2
    >=> validate3
    >=> switch canonicalizeEmail
```

そうするとどうなるか確認してみましょう：

```fsharp
let goodInput = {name="Alice"; email="UPPERCASE   "}
usecase goodInput
|> printfn "正規化された正常な結果 = %A"

// 正規化された正常な結果 = Success {name = "Alice"; email = "uppercase";}

let badInput = {name=""; email="UPPERCASE   "}
usecase badInput
|> printfn "正規化された不正な結果 = %A"

// 正規化された不正な結果 = Failure "名前を入力してください"
```

## 1路線関数から2路線関数を作成する ##

先ほどの例では1路線関数を元にしてスイッチ関数を作成しました。
そうすることによって、スイッチ合成できるようになったわけです。

しかし場合によっては2路線モデルを直接使用して、1路線関数を2路線関数に直接変換したいということもあるでしょう：

![1路線関数を直接2路線関数に変換する][link28]

この場合もやはり、単純な関数をスロットにもつようなアダプターブロックが必要です。
このようなアダプターを一般的に``map``と呼んでいます。

![mapというアダプター][link29]

今回もやはり直感的に実装できます。
2路線入力が``Success``の場合には関数を呼び出して、結果をSuccessとして返すだけです。
一方、入力が``Failure``だった場合には関数を完全にスキップします。

コードは以下の通りです：

```fsharp
// 通常の関数を2路線関数に変換します
let map oneTrackFunction twoTrackInput =
    match twoTrackInput with
    | Success s -> Success (oneTrackFunction s)
    | Failure f -> Failure f
```

``canonicalizeEmail``と組み合わせると以下のようになります：

```fsharp
let usecase =
    validate1
    >=> validate2
    >=> validate3
    >> map canonicalizeEmail // 通常の合成
```

``map canonicalizeEmail``は完全に2路線の関数を返し、``validate3``スイッチの出力と直接連結させることができるため、通常の合成を使用している点に注意してください。

別の言い方をすると、1路線関数に対しては``>=> switch``と``>> map``が全く同じ機能をするということです。

## 行き止まり関数を2路線関数に変換する ##

使用頻度の高いものとしては、もう1つ「行き止まり」関数があるでしょう。
これはつまり入力を受け付けるものの、有効な出力を行わないようなものです。

たとえばデータベースのレコードを更新する関数を考えてみましょう。
この関数は副作用を起こすことだけが重要で、通常は特に返り値を返しません。

こういった関数をどうすればフローの中に組み込めるでしょうか？

必要な処理は以下の通りです：

* 入力のコピーを保存する
* 関数を呼び出して、それが出力を持つなら出力を無視する
* 元々の入力をチェインの次の関数に渡す

鉄道用語でいえば、以下のように行き止まり用の待避路線を用意することになります。

![行き止まり用の待避路線][link30]

これが機能するには、``switch``のようなまた新しいアダプター関数を用意する必要があります。
ただし今回は1路線の行き止まり関数用のスロットを持ち、行き止まり関数を1路線入出力のパススルー関数に変換するようなものになります。

![行き止まり関数用のアダプター][link31]

コードは以下の通りで、UNIXのteeコマンドにならって``tee``と名付けています：

```fsharp
let tee f x =
    f x |> ignore
    x
```

これで行き止まり関数を単純な1路線パススルー関数に変換できるようになったので、先に説明した``switch``あるいは``map``を使用してデータフローに追加できます。

「スイッチ合成」スタイルのコードだと以下のようになります：

```fsharp
// 行き止まり関数
let updateDatabase input =
    () // 今のところはダミー

let usecase =
    validate1
    >=> validate2
    >=> validate3
    >=> switch canonicalizeEmail
    >=> switch (tee updateDatabase)
```

あるいは``switch``と``>=>``の代わりに``map``と``>>``を使用することもできます。

通常の合成を使用する「2路線」スタイルの場合だと以下のような実装になります。

```fsharp
let usecase =
    validate1
    >> bind validate2
    >> bind validate3
    >> map canonicalizeEmail
    >> map (tee updateDatabase)
```

## 例外処理 ##

このデータベース更新関数は何も値を返さないかもしれませんが、かといってそれが例外をスローしないというわけではありません。
例外時にはクラッシュしてしまうのではなく、例外をキャッチしてそれを失敗として通知したいはずです。

コードは``switch``に似ていますが、例外をキャッチしているという違いがあります。
この関数を``tryCatch``と名付けることにしましょう：

```fsharp
let tryCatch f x =
    try
        f x |> Success
    with
    | ex -> Failure ex.Message
```

データベース更新用の関数に対しては``switch``の代わりに``tryCatch``を使用した場合のコードは以下のようになります。

```fsharp
let usecase =
    validate1
    >=> validate2
    >=> validate3
    >=> switch canonicalizeEmail
    >=> tryCatch (tee updateDatabase)
```

## 2路線入力の関数 ##

これまでの関数はいずれも成功パスにおいてしか機能しないものばかりだったので、どの関数も1つの入力しか受け付けませんでした。

しかし両方の路線を絶対に処理しなければならないような関数が必要になることもあります。
たとえばログ処理関数は成功も失敗もどちらともログとして残さなければいけません。

今回もやはりアダプター関数を作成することになります。
ただし今回は1路線関数用のスロットを2つ持てるようにします。

![2つの1路線関数用スロットを持つアダプター][link32]

コードは以下の通りです：

```fsharp
let doubleMap successFunc failureFunc twoTrackInput =
    match twoTrackInput with
    | Success s -> Success (successFunc s)
    | Failure f -> Failure (failureFunc f)
```

ちなみに失敗用の関数に``id``を使用すると、``map``のシンプルバージョンをこの関数で作成できます：

```fsharp
let map successFunc =
    doubleMap successFunc id
```

では``doubleMap``を使用して、ログ処理をデータフローに組み込んでみましょう：

```fsharp
let log twoTrackInput =
    let success x = printfn "DEBUG. 今のところ問題なし: %A" x; x
    let failure x = printfn "ERROR. %A" x; x
    doubleMap success failure twoTrackInput

let usecase =
    validate1
    >=> validate2
    >=> validate3
    >=> switch canonicalizeEmail
    >=> tryCatch (tee updateDatabase)
    >> log
```

テストコードとその結果は以下のようになります：

```fsharp
let goodInput = {name="Alice"; email="good"}
usecase goodInput
|> printfn "良い結果 = %A"

// DEBUG. 今のところ問題なし: {name = "Alice"; email = "good";}
// 良い結果 = Success {name = "Alice"; email = "good";}

let badInput = {name=""; email=""}
usecase badInput
|> printfn "悪い結果 = %A"

// ERROR. "名前を入力してください"
// 悪い結果 = Failure "名前を入力してください"
```

[link01]: http://fsharpforfunandprofit.com/posts/recipe-part2/ "Railway oriented programming"
[link02]: img/01-10.png "Figure 01-10.png"
[link03]: img/02-01.png "Figure 02-01.png"
[link04]: img/02-02.png "Figure 02-02.png"
[link05]: img/02-03.png "Figure 02-03.png"
[link06]: img/02-04.png "Figure 02-04.png"
[link07]: img/02-05.png "Figure 02-05.png"
[link08]: img/02-06.png "Figure 02-06.png"
[link09]: img/02-07.png "Figure 02-07.png"
[link10]: img/02-08.png "Figure 02-08.png"
[link11]: img/02-09.png "Figure 02-09.png"
[link12]: img/02-10.png "Figure 02-10.png"
[link13]: img/02-11.png "Figure 02-11.png"
[link14]: img/02-12.png "Figure 02-12.png"
[link15]: img/02-13.png "Figure 02-13.png"
[link16]: img/02-14.png "Figure 02-14.png"
[link17]: img/02-15.png "Figure 02-15.png"
[link18]: img/02-16.png "Figure 02-16.png"
[link19]: img/02-17.png "Figure 02-17.png"
[link20]: img/02-18.png "Figure 02-18.png"
[link21]: img/02-19.png "Figure 02-19.png"
[link22]: img/02-20.png "Figure 02-20.png"
[link23]: img/02-21.png "Figure 02-21.png"
[link24]: img/02-22.png "Figure 02-22.png"
[link25]: img/02-23.png "Figure 02-23.png"
[link26]: img/02-24.png "Figure 02-24.png"
[link27]: img/02-25.png "Figure 02-25.png"
[link28]: img/02-26.png "Figure 02-26.png"
[link29]: img/02-27.png "Figure 02-27.png"
[link30]: img/02-28.png "Figure 02-28.png"
[link31]: img/02-29.png "Figure 02-29.png"
[link32]: img/02-30.png "Figure 02-30.png"
