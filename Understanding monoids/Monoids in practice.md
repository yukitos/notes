# 実践モノイド
数学的な知識をほぼ必要とせずに一般的な関数パターンを説明する

原文：[Monoids in practice][link01]

----

[前回][link02] の記事ではモノイドの定義について説明しました。
今回は一部のモノイドの実装方法について説明します。

まず定義を再度確認しておきます：

* 一連のものがあって、これらのうち2つを一度に連結するような方法がある
* 規則1 (クロージャ)：2つのものを連結した結果は常に別のものになる
* 規則2 (結合性)：2つ以上のものを連結する場合、最初に連結する対はどれでも構わない
* 規則3 (単位元)：別のものと連結しても常に別のものが返されるような「ゼロ」と呼ばれる特別なものがある

たとえばあるものが文字列だとして、演算子が連結だとするとこれらはモノイドです。
具体的なコードとしては以下のようになります：

```fsharp
let s1 = "hello"
let s2 = " world!"

// クロージャ
let sum = s1 + s2 // sumは文字列

// 結合性
let s3 = "x"
let s4a = (s1+s2) + s3
let s4b = s1 + (s2+s3)
assert (s4a = s4b)

// 空文字列が単位元
assert (s1 + "" = s1)
assert ("" + s1 = s1)
```

しかし今回はもう少し複雑なオブジェクトに応用してみます。

たとえば販売注文の1品目を表す次のような小さな構造体``OrderLine``があるとします。

```fsharp
type OrderLine = {
    ProductCode: string
    Qty: int
    Total: float
    }
```

そしておそらくは1つの注文に対する総額を計算したいはずで、
その場合には注文品目のリストにある ``Total`` の総和を計算することになります。

普通に命令的アプローチで実装する場合、
以下のように ``total`` というローカル変数を用意しておいて
各品目をループしつつ足していくことになるでしょう：

```fsharp
let calculateOrderTotal lines =
    let mutable total = 0.0
    for line in lines do
        total <- total + line.Total
    total
```

試してみましょう：

```fsharp
module OrdersUsingImperativeLoop =

    type OrderLine = {
        ProductCode: string
        Qty: int
        Total: float
        }

    let calculateOrderTotal lines =
        let mutable total = 0.0
        for line in lines do
            total <- total + line.Total
        total

    let orderLines = [
        { ProductCode="AAA"; Qty=2; Total=19.98 }
        { ProductCode="BBB"; Qty=1; Total=1.99 }
        { ProductCode="CCC"; Qty=3; Total=3.99 }
        ]

    orderLines
    |> calculateOrderTotal
    |> printfn "総額は %g"
```

しかしもちろん熟練の関数プログラマであれば
このコードをあざ笑いつつ、以下のように
``fold`` を使って ``calculateOrderTotal`` を書き換えるでしょう：

```fsharp
module OrdersUsingImperativeLoop =

    type OrderLine = {
        ProductCode: string
        Qty: int
        Total: float
        }

    let calculateOrderTotal lines =
        let accumulateTotal total line =
            total + line.Total
        lines
        |> List.fold accumulateTotal 0.0

    let orderLines = [
        { ProductCode="AAA"; Qty=2; Total=19.98 }
        { ProductCode="BBB"; Qty=1; Total=1.99 }
        { ProductCode="CCC"; Qty=3; Total=3.99 }
        ]

    orderLines
    |> calculateOrderTotal
    |> printfn "総額は %g"
```

ここまでは順調です。
ではモノイドアプローチを使った解法を見てみましょう。

モノイドの場合、加算あるいは連結演算子のようなものを定義する必要があります。
こんな感じでどうでしょう？

```fsharp
let addLine orderLine1 orderLine2 =
    orderLine1.Total + orderLine2.Total
```

しかしこれではモノイドの重要な性質が損なわれてしまっているので駄目です。
加算は必ず同じ型を返さなければいけません！

``addLine`` 関数のシグネチャは以下のようになっています：

```fsharp
addLine : OrderLine -> OrderLine -> float
```

このように、 ``OrderLine`` ではなく ``float`` が返り値の型になっています。

つまり全く別の ``OrderLine`` を返すようにする必要があります。
正しい実装は以下の通りです：

```fsharp
let addLine orderLine1 orderLine2 =
    {
    ProductCode = "TOTAL"
    Qty = orderLine1.Qty + orderLine2.Qty
    Total = orderLine1.Total + orderLine2.Total
    }
```

これでシグネチャも正しくなりました：``addLine : OrderLine -> OrderLine -> OrderLine``

なお完全な構造体を返す必要があるため、 ``Total`` だけではなく
``ProductCode`` や ``Qty`` についても何かしら値を
設定する必要がある点に注意してください。
``Qty`` は単に和を設定すれば良いので簡単です。
``ProductCode`` は今のところ実在する製品コードを使えるわけではないので
とりあえず「TOTAL」としておきました。

簡単なテストをしてみましょう：

```fsharp
// OrderLineを出力するユーティリティメソッド
let printLine { ProductCode=p; Qty=q; Total=t } =
    printfn "%-10s %5i %6g" p q t

let orderLine1 = { ProductCode="AAA"; Qty=2; Total=19.98 }
let orderLine2 = { ProductCode="BBB"; Qty=1; Total=1.99 }

// 3つめのものを作るために2つを加算
let orderLine3 = addLine orderLine1 orderLine2
orderLine3 |> printLine // そしてそれを出力
```

結果は以下のようになります：

```fsharp
TOTAL          3  21.97
```

> 注釈：printfの書式オプションの詳細については [こちらのprintfに関する記事][link03] を参照してください。

では ``reduce`` を使ってこの関数をリストに適用してみましょう：

```fsharp
let orderLines = [
    { ProductCode="AAA"; Qty=2; Total=19.98 }
    { ProductCode="BBB"; Qty=1; Total=1.99 }
    { ProductCode="CCC"; Qty=3; Total=3.99 }
    ]

orderLines
|> List.reduce addLine
|> printLine
```

結果は以下の通りです：

```fsharp
TOTAL          6  25.96
```

最初のうちはこれが余計な処理をしていて、総額を計算しているように見えるかもしれません。
しかし実際のところは単に総額だけではなく、それ以上の情報、つまり総量についても
計算できているという点に注意してください。

たとえば ``printLine`` 関数を再利用して、
以下のような総額も含んだレシート印刷用の関数を作ることもできます：

```fsharp
let printReceipt lines =
    lines
    |> List.iter printLine

    printfn "-----------------------"

    lines
    |> List.reduce addLine
    |> printLine

orderLines
|> printReceipt
```

出力結果は以下のようになります：

```fsharp
AAA            2  19.98
BBB            1   1.99
CCC            3   3.99
-----------------------
TOTAL          6  25.96
```

さらに重要な点として、モノイドのインクリメント可能な性質を利用することにより、
新しい品目を追加する度に小計を更新することができます。

たとえば以下のような場合です：

```fsharp
let subtotal = orderLines |> List.reduce addLine
let newLine = { ProductCode="DDD"; Qty=1; Total=29.98 }
let newSubtotal = subtotal |> addLine newLine
newSubtotal |> printLine
```

各品目をあたかも数値と同じように足しあわせることができるように、
独自の演算子 ``++`` を定義することもできます：

```fsharp
let (++) a b = addLine a b

let newSubtotal = subtotal ++ newLine
```

このように、モノイドパターンを使うと全く新しい考え方が出来るようになります。
今回のような「足し算」のアプローチはほとんどすべてのオブジェクトに適用できます。

たとえば製品「足す」製品はどうなるでしょう？
あるいは顧客「足す」顧客は？
想像力を自由に働かせてみてください！

## もう終わり？

皆さんはきっとまだ全然説明が終わってないと思っていることでしょう。
モノイドの3つめの条件、ゼロあるいは単位元についての説明がまだ残っています。

今回の場合、注文品目に追加をしても元の注文品目が返されるような
``OrderLine`` が必要です。
有りますか？

今のところはありません。
というのも、加算演算子は常に製品コードを「TOTAL」にしてしまうからです。
したがってこれはモノイドではなくて **半群** です。

既に説明したように、半群も十分有用です。
しかし空の注文品目に対する総額を計算しようとすると問題が起こります。
どのような結果になるべきでしょう？

1つの回避策としては、 ``addLine`` 関数を変更して空の製品コードを
無視するようにする方法です。
そうすれば空の製品コードを持った注文品目がゼロ要素として機能するようになります。

つまりこういうことです：

```fsharp
let addLine orderLine1 orderLine2 =
    match orderLine1.ProductCode, orderLine2.ProductCode with
    // どちらかがゼロ？そうであれば他方を返します
    | "", _ -> orderLine2
    | _, "" -> orderLine1
    // その他の場合は以前と同じです
    | _ ->
        {
        ProductCode = "TOTAL"
        Qty = orderLine1.Qty + orderLine2.Qty
        Total = orderLine1.Total + orderLine2.Total
        }

let zero = { ProductCode=""; Qty=0; Total=0.0 }
let orderLine1 = { ProductCode="AAA"; Qty=2; Total=19.98 }
```

この単位元が正しく機能しているかどうかテストしてみましょう：

```fsharp
assert (orderLine1 = addLine orderLine1 zero)
assert (orderLine1 = addLine zero orderLine1)
```

この方法は少し場当たり的なので、一般的にこのテクニックを採用することはおすすめしません。
後ほど説明しますが、単位元を作る別の方法もあります。

## 特別な総額用の型を用意する

以上の例にある ``OrderLine`` 型は非常に単純で、総額のフィールドを簡単に上書きできます。

しかし ``OrderLine`` 型がもっと複雑だった場合はどうでしょう？
たとえば以下のように ``Price`` フィールドを含んでいたとします：

```fsharp
type OrderLine = {
    ProductCode: string
    Qty: int
    Price: float
    Total: float
    }
```

さてそうすると面倒なことになります。
2つの品目を足しあわせるときに ``Price`` には何を設定すればいいでしょう？
価格の平均値？
あるいは価格無し？

```fsharp
let addLine orderLine1 orderLine2 =
    {
    ProductCode = "TOTAL"
    Qty = orderLine1.Qty + orderLine2.Qty
    Price = 0 // あるいは平均価格？
    Total = orderLine1.Total + orderLine2.Total
    }
```

どちらも正しくなさそうです。
どうすべきかわからないということは、おそらく設計が間違っているということなのでしょう。

実際、合計を出すだけであればすべてのデータではなく一部のデータだけで十分です。
これをどうやって表現しましょう？

もちろん判別共用体ですね！
1つは製品品目、そしてもう1つは合計だけを表すようにします。

つまりこういうことです：

```fsharp
type ProductLine = {
    ProductCode: string
    Qty: int
    Price: float
    LineTotal: float
    }

type TotalLine = {
    Qty: int
    OrderTotal: float
    }

type OrderLine =
    | Product of ProductLine
    | Total of TotalLine
```

こちらの設計のほうがだいぶいい感じです。
今回は合計用の特別な構造体も追加したので、
データを無理矢理調整する必要もなくなっています。
また、「TOTAL」というダミーの製品コードも不要です。

> 各レコードの「合計」フィールドにはそれぞれ別の名前をつけている点に注意してください。
> このように別の名前をつけておけば、常に明示的に型を指定する必要がなくなります。

それぞれの判別子に応じて処理を行わないといけなくなったので、
残念ながら足し算のロジックはかなり複雑になります：

```fsharp
let addLine orderLine1 orderLine2 =
    let totalLine =
        match orderLine1, orderLine2 with
        | Product p1, Product p2 ->
            { Qty = p1.Qty + p2.Qty
              OrderTotal = p1.LineTotal + p2.LineTotal }
        | Product p, Total t ->
            { Qty = p.Qty + t.Qty
              OrderTotal = p.LineTotal + t.OrderTotal }
        | Total t, Product p ->
            { Qty = p.Qty + t.Qty
              OrderTotal = p.LineTotal + t.OrderTotal }
        | Total t1, Total t2 ->
            { Qty = t1.Qty + t2.Qty
              OrderTotal = t1.OrderTotal + t2.OrderTotal }
    Total totalLine // totalLineがOrderLineになるようにラップします
```

なお ``TotalLine`` の値をそのまま返すことができない点に注意してください。
ここでは ``Total`` ケースを使って正しく ``OrderLine`` になるようにしています。
もしこれを省略してしまうと ``addLine`` のシグネチャが
``OrderLine -> OrderLine -> TotalLine`` と間違った形になってしまいます。
``OrderLine -> OrderLine -> OrderLine`` というシグネチャが必要です。
それ以外は駄目です！

さて2つのケースに分かれるようになったため、
``printLine`` 関数もそれぞれに対応するよう変更する必要があります：

```fsharp
let printLine = function
    | Product { ProductCode=p; Qty=q; Price=pr; LineTotal=t } ->
        printfn "%-10s %5i 単価 %4g 合計 %6g" p q pr t
    | Total { Qty=q; OrderTotal=t } ->
        printfn "%-10s %5i                %6g" "TOTAL" q t
```

ここまで来れば以前と同じように足せるようになります：

```fsharp
let orderLine1 = Product { ProductCode="AAA"; Qty=2; Price=9.99; LineTotal=19.98 }
let orderLine2 = Product { ProductCode="BBB"; Qty=1; Price=1.99; LineTotal=1.99 }
let orderLine3 = addLine orderLine1 orderLine2

orderLine1 |> printLine
orderLine2 |> printLine
orderLine3 |> printLine
```

## 再び単位元

今回もまだ単位元について説明していません。
以前と同じく、空の製品コードを使えばよさそうですが、
そうすると ``Product`` ケースだけにしか機能しません。

正しい単位元を用意するためには、 **3** 番目のケースとして
``EmptyOrder`` を追加する必要があります。

```fsharp
type ProductLine = {
    ProductCode: string
    Qty: int
    Price: float
    LineTotal: float
    }

type TotalLine = {
    Qty: int
    OrderTotal: float
    }

type OrderLine =
    | Product of ProductLine
    | Total of TotalLine
    | EmptyOrder
```

そしてこの新しく追加したケースが処理されるように ``addLine`` 関数を書き換えます：

```fsharp
let addLine orderLine1 orderLine2 =
    match orderLine1, orderLine2 with
    // どちらかがゼロ？その場合は他方を返す
    | EmptyOrder, _ -> orderLine2
    | _, EmptyOrder -> orderLine1
    // その他については以前と同じ
    | Product p1, Product p2 ->
        Total { Qty = p1.Qty + p2.Qty;
          OrderTotal = p1.LineTotal + p2.LineTotal }
    | Product p, Total t ->
        Total { Qty = p.Qty + t.Qty;
          OrderTotal = p.LineTotal + t.OrderTotal }
    | Total t, Product p ->
        Total { Qty = p.Qty + t.Qty;
          OrderTotal = p.LineTotal + t.OrderTotal }
    | Total t1, Total t2 ->
        Total { Qty = t1.Qty + t2.Qty;
          OrderTotal = t1.OrderTotal + t2.OrderTotal }
```

以下のようにテストしてみます：

```fsharp
let zero = EmptyOrder

// 単位元のテスト
let productLine = Product { ProductCode="AAA"; Qty=2; Price=9.99; LineTotal=19.98 }
assert ( productLine = addLine productLine zero )
assert ( productLine = addLine zero productLine )

let totalLine = Total { Qty=2; OrderTotal=19.98 }
assert ( totalLine = addLine totalLine zero )
assert ( totalLine = addLine zero totalLine )
```

## 組み込みのList.sum関数を使う

実のところ、 ``List.sum`` 関数はモノイドのことを知っていたのです！
この関数に足し算の演算子が何で、ゼロが何かということを教えてあげると
``List.fold`` の代わりに ``List.sum`` を直接使うことができます。

そこで以下のような ``+`` と ``Zero`` という2つの静的メンバを
型に追加します：

```fsharp
type OrderLine with
    static member (+) (x,y) = addLine x y
    static member Zero = EmptyOrder
```

そうすると ``List.sum`` が期待通りに動作するようになります：

```fsharp
let lines1 = [productLine]
// 明示的に演算子とゼロを指定してfoldを呼ぶ
lines1 |> List.fold addLine zero |> printfn "%A"
// 暗黙的に演算子とゼロを使ってmapを呼ぶ
lines1 |> List.sum |> printfn "%A"

let emptyList: OrderLine list = []
// 明示的に演算子とゼロを指定してfoldを呼ぶ
emptyList |> List.fold addLine zero |> printfn "%A"
// 暗黙的に演算子とゼロを使ってmapを呼ぶ
emptyList |> List.sum |> printfn "%A"
```

なおこのコードが正しく動作するためには、 ``Zero`` という名前の
既存のメソッドやケースが存在してはいけません。
``EmptyOrder`` の代わりに ``Zero`` という名前にしていたら
動作しなかったでしょう。

分かりづらい話ではありますが、たとえば ``ComplexNumber`` や ``Vector`` のような
数学に関連する型を作成していない限り、この名前が採用されることはないと思います。
もし採用していたのであれば、筆者の感覚的には少しやり過ぎな感じがします。

さてこのトリックを実現する場合、 ``Zero`` メンバは
拡張メソッドにはできない点に注意してください。
必ず型のメンバとして定義する必要があります。

たとえば以下のコードでは文字列用の「ゼロ」を定義しようとしています。

``List.fold`` には拡張メソッドとして定義された ``String.Zero`` が見えているので
正しく機能しますが、 ``List.sum`` からは拡張メソッドが見えないので
エラーになります。

```fsharp
module StringMonoid =

    // 拡張メソッドを定義
    type System.String with
        static member Zero = ""

    // OK
    ["a"; "b"; "c"]
    |> List.reduce (+)
    |> printfn "reduceの場合：%s"

    // OK。拡張メソッドであるString.Zeroは見えている
    ["a"; "b"; "c"]
    |> List.fold (+) System.String.Zero
    |> printfn "foldの場合：%s"

    // エラー。List.sumからはString.Zeroが見えない
    ["a"; "b"; "c"]
    |> List.sum
    |> printfn "sumの場合：%s"
```

## 別の構造体にマッピングする

判別共用体として2つのケースを用意したおかげで
注文品目用のケースを持てるようになったわけですが、
現実的なコードの場合、この方法はあまりに複雑すぎるか、
あるいは紛らわしいものです。

以下のような顧客レコードがあるとします：

```fsharp
open System

type Customer = {
    Name: string // および多数の文字列フィールド！
    LastActive: DateTime
    TotalSpend: float }
```

どうやって二人の顧客を「足し算」したらいいでしょうか？

目安としては、集計機能は実際のところ数字あるいは数字的な型に対してしか
機能しないという点に注目します。
文字列は簡単には集計できません。

したがって ``Customer`` を集計するのではなく、
すべての集計情報を含むような ``CustomerStats`` を別に定義するとよいでしょう：

```fsharp
// 顧客の統計情報を追跡するための型を作成する
type CustomerStats = {
    // 以下の統計情報に含まれる顧客の数
    Count: int
    // 最後の利用日からの経過日数の合計
    TotalInactiveDays: int
    // 総額
    TotalSpend: float }
```

``CustomerStats`` のフィールドはどれも数値なので、
2つの統計情報を簡単に足し算できます：

```fsharp
let add stat1 stat2 = {
    Count = stat1.Count + stat2.Count
    TotalInactiveDays = stat1.TotalInactiveDays + stat2.TotalInactiveDays
    TotalSpend = stat1.TotalSpend + stat2.TotalSpend
    }

// 中間演算子バージョンも定義する
let (++) a b = add a b
```

これまでと同じく、 ``add`` 関数の入力と出力は同じ型でなければいけません。
つまり ``Customer -> Customer -> CustomerStats`` などではなく、
``CustomerStats -> CustomerStats -> CustomerStats`` でなければいけないのです。

OK。ここまではいいでしょう。

次は顧客のコレクションがあって、そこから統計情報を集計するには
どうしたらいいでしょうか？

顧客をそのまま足しあわせることはできないので、
まずはそれぞれの顧客を ``Customerstats`` に変換して、
その後にモノイド演算子を使って統計情報を足していくことになります。

たとえば以下のようになります：

```fsharp
// 顧客を統計情報に変換
let toStats cust =
    let inactiveDays = DateTime.Now.Subtract(cust.LastActive).Days;
    { Count=1; TotalInactiveDays=inactiveDays; TotalSpend=cust.TotalSpend }

// 顧客のリストを作成
let c1 = { Name="Alice"; LastActive=DateTime(2005,1,1); TotalSpend=100.0 }
let c2 = { Name="Bob"; LastActive=DateTime(2010,2,2); TotalSpend=45.0 }
let c3 = { Name="Charlie"; LastActive=DateTime(2011,3,3); TotalSpend=42.0 }
let customers = [c1;c2;c3]

// 統計情報を集計
customers
|> List.map toStats
|> List.reduce add
|> printfn "結果 = %A"
```

まず、 ``toStats`` は1人の顧客に対する統計情報しか作成しないことに注意してください。
そのためカウントを1に設定しています。
これは不思議に思えるかもしれませんがこれでいいのです。
リストに1人しか顧客が含まれなければ、集計後の統計情報には
まさにその1人しか含まれないわけです。

また、集計後の結果にも注意してください。
まず ``map`` を使って元の型をモノイド型に変換した後、
``reduce`` を使ってすべての統計情報を集計しています。

んー、、、``map`` した後に ``reduce``。
何か思い出しませんか？

そうです。かの有名なGoogleのMapReduceアルゴリズムは
まさにこのコンセプトに沿ったものなのです
(細かい部分ではやや違うものですが)。

説明を続ける前に理解力テストを出題することにしましょう。

* ``CustomerStats`` にとっての「ゼロ」は何でしょう？
  空のリストに対して ``List.fold`` を使ってコードをテストしてみましょう。
* 単純な ``OrderStats`` クラスを作成して、このクラスを使って
  今回の記事の先頭で定義した ``OrderLine`` 型を集計してみましょう。

## モノイド準同型



[link01]: http://fsharpforfunandprofit.com/posts/monoids-part2/ "Monoids in practice"
[link02]: Monoids%20without%20tears.md "難しくないモノイド"
[link03]: http://fsharpforfunandprofit.com/posts/printf/ "Formatted text using printf"
