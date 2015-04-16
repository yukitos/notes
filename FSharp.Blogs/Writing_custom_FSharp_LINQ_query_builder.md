# 独自のF# LINQクエリビルダーを作成する

> 原文：[Writing custom F# LINQ query builder][link1]

私が開催している [F# in Finance][link2] コースの参加者である[Stuart][link3] から、
F#で独自のクエリを作成するにはどうしたらいいかという、
少し難しい質問を最近受けました。
というのも、彼はAmazon DynamoDB用のいい感じなクエリライブラリを
作成しようと思っているとのことでした(彼の[プロジェクトはこちら][link4])。

そういえば確かにクエリビルダーを作成する際に参考となるような、
よいリソースを知らないということに気がついたので、
それならばとこうして記事を書く事にしたというわけです。
アイディアとしては以下のようなコードを書けるようにしたいところです：

```fsharp
query {
    for p in DynamoDB.People do
    where (p.Age > 10)
    where (p.Name = "Tomas")
    select (p.Age, p.Name) }
```

`DynamoDB` は(Dynamo DB上で利用可能なすべてのテーブルを利用できるように)
型プロバイダーで生成された型とすることもできるでしょう。
上の例では組み込みの`query`ビルダーを使っていて、
これを拡張していくことも可能ではあります。
しかしその場合、私が知る限りではLINQの式ツリーを使わないといけないはずです。
そこで今回の記事では別の方法として、独自のビルダーを作成していくことにします
(つまり`query { ... }`ではなく`dynamo { ... }`というコードになります)。

独自のビルダーを作成するために必要な最低限のサンプルを紹介しようと思っているので、
今回の記事は(私が書いた説明の多い他の記事とは違って！)
ほとんどがコードの紹介になる予定ですが、
きっと皆さんの(そしてStuartにも :-))
役に立つのではないかと思います。
DynamoDB用のナイスなクエリ言語を用意するというアイディアには
とても共感するところがあるので、この記事がプロジェクトの推進に
役立つ事を期待しています！

## 型およびコンピュテーションビルダーを定義する

まず最初にデータベースをモデル化する型と、
`query { ... }`のように記述できるようにするためのクエリビルダーを
定義する必要があります。
今回はこのビルダーを`simpleq`とします。
これらの定義には紹介していないコードが含まれることになる可能性もありますが、
実装を**全く持たない**ものでも構いません。
実際、今回はいずれのメソッドも呼び出しません！
その代わり、クエリのF#**クォーテーション**をキャプチャして、
クエリを表すような単純なデータ構造へと変換することにします。

というわけで、型が`'T`である値を返すようなクエリを表す`Query<'T>`を定義します
(ただし実際には何もしません)。
またサンプル用に`Person`型と、データベースを表す`DB`型も定義します：

```fsharp
open Microsoft.FSharp.Quotations

/// クエリを表します(ただし決して実行されません!)
type Query<'T> = NA

/// データベースからインポートされるはずのサンプル用テーブル
type Person =
    { Name : string
      Age : int }

/// 1つの'People'テーブルを持つようなデータベースモデル
type DB =
    static member People : Query<Person> = NA
```

次に、クエリ内で実行可能な操作を定義するために、
コンピュテーションビルダーを定義します。
これはコンパイラによって認識される特別な名前を持ったメンバー
(および特定の属性でマークされているメンバー)を含むオブジェクトです。
導入部で紹介したようなクエリを記述すると、
コンパイラーはコンピュテーションビルダーの
各操作を呼び出すコードへとクエリを変換します。

クエリの変換にクォーテーションを使用しない場合には、
コンピュテーションビルダー自身に変換処理を実装することになります。
しかし今回の場合、実装自体は空のままで問題ありません。

ビルダーには`For`と`Yield`を実装します。
これらはすべてのクエリに必要です。
そしてクォーテーションをキャプチャする必要があるということを
コンパイラに伝えられるよう、`Quote`も定義します
(つまりいちいち面倒くさい`<@ .. @>`を使わなくてもいいと言う事です！)。
残る3つの操作はクエリ内で使用できる独自の操作を定義するものです：

```fsharp
/// 'for', 'where', 'selectAttr', 'selectCount' の操作が可能な
/// クエリを作成できるようなクエリビルダを定義します。
type SimpleQueryBuilder() =
    /// 'For'と'Yield'により、'for x in xs do ...'構文が使えるようになります
    member x.For(tz:Query<'T>, f:'T -> Query<'R>) : Query<'R> = NA
    member x.Yield(v:'T) : Query<'T> = NA

    /// クエリのクォーテーションをキャプチャするようコンパイラに伝えます
    member x.Quote(e:Expr<_>) = e

    /// 特定の条件でソースをフィルタする操作を表します
    [<CustomOperation("where", MaintainsVariableSpace=true)>]
    member x.Where
        ( source:Query<'T>,
          [<ProjectionParameter>] f:'T -> bool ) : Query<'T> = NA

    /// 選択した特定のプロパティに対する射影操作を表します
    [<CustomOperation("selectAttrs")>]
    member x.SelectAttrs
        ( source:Query<'T>,
          [<ProjectionParameter>] f:'T -> 'R) : Query<'R> = NA

    /// 取得した値の個数に対する射影操作を表します
    [<CustomOperation("selectCount")>]
    member x.SelectCount(source:Query<'T>) : int =
        failwith "実行されません"

/// コンピュテーションビルダーのグローバルインスタンス
let simpleq = SimpleQueryBuilder()
```

必須の操作である`Yield`と`For`の他に、
`CustomOperation`属性が指定された3つの操作を独自に定義しています。
なお独自の操作は必要な分だけ任意個定義できます。
興味深いところがいくつかあります：

* `where`操作は`Query<'T>`を`Query<'T>`に変換するもので、
  `MaintainsVariableSpace=true`が指定されている
  (これはつまりクエリが生成する値の型を変更せず、
  単にフィルタするだけだということを表します)
* `selectAttrs`操作は引数として射影をとります。
  なお`selectAttrs (p.Name, p.Age)`というようにクエリを記述できるようにするには
  `ProjectionParameter`属性を指定する必要があります。
* `selectCount` 操作は単純で、追加の引数もとりません
  (しかし必要に応じて引数を追加することもできます。
  たとえば整数を引数にとる標準的な`take`操作を用意して、
  `take 10`と書けるようにすることもできます)

スニペットは`simpleq`という値の定義で終わっています。
これは`simpleq { ... }`というコードを書いた際に使われることになる
クエリビルダーのインスタンスで、たとえば以下のようなクエリが書けます：

```fsharp
let q =
    simpleq {
        for p in DB.People do
        where (p.Age > 10)
        where (p.Name = "Tomas")
        selectCount }
```

今回は`Quote`操作を定義しているので、`q`の型は1つの式を表す
`Expr<int>`になっています。
実際の例では、クエリを実行することができるように`Expr<'T> -> 'T`型の
`Run`操作を追加することになるでしょう。
ここでは単に変換処理だけを見ていく事にします
(なお評価がどのように行われるのかという点については議論しません)。

## クエリモデルを定義する

クエリの変換について説明する前に、サンプルのクエリから展開したい情報について
説明することにします。
組み立てたいのは、実行されることになる操作の種類をを表す、
以下のような`Query`型の値です：

```fsharp
/// p.<Property> <op> <Constant>
/// という形式の単一条件を定義します
type QueryCondition =
     { Property : string
       Operator : string
       Constant : obj }

/// クエリの最後に発生する射影の種類を指定します
/// (カウントする、または射影後の属性リストを返す)
type QueryProjection =
    | SelectAttributes of string list
    | SelectCount

/// ソースとなる(テーブル)名と、'where'で指定されたフィルタのリストと、
/// 最後に指定される省略可能な射影操作に対応するクエリ。
type Query =
    { Source : string
      Where : QueryCondition list
      Select : QueryProjection option }
```

ドメインモデルはこの通り見たままです！
1つ以上の`where`句を記述できます。
今回の例では左からプロパティ、演算子、定数という並びの
非常に限定的な形式の条件文しか指定できません。
最後の`select`句は省略可能なので、`Query`内でも`Select`プロパティには
`option`を指定しています。

## クォーテーションを使用してクエリを変換する

さていよいよ今回の記事の本題です。
果たしてどうすればクエリを表すクォーテーション(値`q`)から、
今回のモデルである`Query`型の値へと変換できるのでしょうか。
F# Interactiveを使っているのであれば、FSIで`q`と入力して、
果たしてそれがどのようなものなのか確認してみるといいでしょう。
読み方を理解するには少し時間がかかるかもしれませんが、
以下のような出力になっているはずです：

```fsharp
Call (Some (Value (FSI_0080+QueryBuilder)), SelectCount
  [Call (Some (Value (FSI_0080+QueryBuilder)), Where,
    [Call (Some (Value (FSI_0080+QueryBuilder)), For,
      [PropertyGet (None, People, []), (...) ]),
    Lamblda (p,
      Call (None, op_GreaterThan,
        [PropertyGet (Some (p), Age, []), Value(10)]))])])
```

よく見てみると、クエリが呼び出している操作が見つけられるはずです
(今回の例では`where (p.Age > 10)`だけを指定しています)。
出力結果には操作が逆順で現れているので、
クエリは`SelectCount`の呼び出しで終わっています。
その前が`where`の呼び出しになっています
(出力結果の末尾3行で表されているラムダ式が引数として指定されています)。
最後に、4行目から入力が`People`プロパティであることがわかります。

### クエリの変換

サンプルコードの重要なポイントは以下の`translateQuery`関数にあります。
この関数はクォーテーションを引数にとり、
サポートされている操作に対する呼び出しかどうかをチェックしています。
関数の返値はクエリを表す`Query`型の値で、
クエリを(再帰的に)渡り歩いて徐々に結果を組み立てるようになっています。

`For`操作はソースを指定するものなので、すべての起点になっています。
射影およびフィルタ操作はまず`translateQuery`を(式のネストされた部分に対して)
再帰的に呼び出した後、`Query`レコードに情報を追加しています：

```fsharp
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns

let rec translateQuery e =
    match e with
    // simpleq.SelectAttrs(<source>, fun p -> p.Name, p.Age)
    | SpecificCall <@ simpleq.SelectAttrs @>
            (builder, [tTyp; rTyp], [source; proj]) ->
        let q = translateQuery source
        let s = translateProjection proj
        { q with Select = Some(SelectAttributes s) }

    // simpleq.SelectCount(<source>)
    | SpecificCall <@ simpleq.SelectCount @>
            (builder, [tTyp], [source]) ->
        let q = translateQuery source
        { q with Select = Some SelectCount }

    // simpleq.Where(<source>, fun p -> p.Age > 10)
    | SpecificCall <@ simpleq.Where @>
            (builder, [tTyp], [source; cond]) ->
        let q = translateQuery source
        let w = translateWhere cond
        { q with Where = w :: q.Where }

    // simpleq.For(DB.People, <...>)
    | SpecificCall <@ simpleq.For @>
            (builder, [tTyp; rTyp], [source; body]) ->
        let source =
            // 'DB.People'からテーブル名を展開
            match source with
            | PropertyGet(None, prop, []) when
                prop.DeclaringType = typeof<DB> -> prop.Name
            | _ -> failwith "'DB.<Prop>'形式のソースしかサポートしていません！"
        { Source = source; Where = []; Select = None }

    // このコードには到達しないはず
    | e -> failwithf "サポートされていないクエリ操作です: %A" e
```

もしもクォーテーションやアクティブパターンをご存じで無ければ、
あれこれと補足して説明しないといけないかもしれません。
しかし重要なポイントを以下のようにまとめておきましょう：

* `translateQuery`関数は再帰的です。
  `For`を除いたすべての操作には引数に対するネストした(同じ形式の)クエリが含まれます。
  これはコメント中で`<source>`として記述されています。
  従って、まず先に再帰的に`<source>`を処理した後、
  その他の引数から情報を展開するという動作になっています。
* `SpecificCall <@ ... @>`というパターンは、クォーテーションが特定の操作を
  呼び出すコードになっているかどうかをチェックするためのものです。
  マッチした場合、`(builder, [..] args)`というタプルが得られます。
  最後の要素はメソッド呼び出しの引数を表します
  (その他はインスタンスおよび型引数ですが、今回は無視しています)。

なお`translateWhere`と`translateProjection`関数の定義がありませんが、
これらは次の節で説明します。
いずれも比較的単純なものですが、また別のクォーテーションパターンを使っています。

### where句の変換

`translateWhere`は`fun p -> p.Prop <op> <Value>`という形式を解析する関数です。
クォーテーションでは、`Lambda`パターンで関数を探し、
`Call`パターンで呼び出すべき操作を探しています。
なお呼び出される操作が`op_`で始まる名前のメソッドになっていることを確認しています：

```fsharp
let translateWhere = function
    | Lambda(var1, Call (None, op, [left; right])) ->
        match left, right with
        | PropertyGet(Some (Var var2), prop, []), Value(value, _) when
            var1.Name = var2.Name && op.Name.StartsWith("op_") ->
            // 理解できる'where'句を発見。QueryConditionを組み立てよう！
            { Property = prop.Name
              Operator = op.Name.Substring(3)
              Constant = value }
        | e ->
            // サポートされない形式の'where'句
            // (起こりうることなので、もっと有用なエラーを報告するべき)
            failwithf
                "%s\n値: %A"
                ( "'p.Prop <op> <value>'形式の式だけが" +
                  "サポートされています!") e

    // 起こりえない条件です。引数には必ずラムダ式を指定します！
    | _ -> failwith "ラムダ式を指定する必要があります"
```

### select句の変換

select句の変換も同じようにできます。
ここでもラムダ式を探して、射影の結果を反映するようなプロパティのリストとなるように
本体を変換します。

ここでは2種類の本体の式を探します。
`select p.Name`と記述した場合、これはプロパティへのアクセスを本体に持つような
関数として表されます。
これは単一要素のリストとして変換します。
`select (p.Name, p.Age)と記述した場合、タプルを受け取ることになるので、
タプルの要素に結びついた名前のリストへと変換します：

```fsharp
/// プロパティへのアクセスを変換します(タプル内または単に値)
let translatePropGet varName = function
    | PropertyGet(Some(Var v), prop, [])
        // 本体は単純な射影
        when v.Name = varName -> prop.Name
    | e ->
        // 射影が複雑すぎます
        failwithf
            "%s\n値: %A"
            ( "'p.Prop'形式の式だけが" +
              "サポートされています!") e

/// 射影を変換します。
/// この関数は(タプルまたは非タプル)両方の形式を処理して
/// 'translatePropGet'を呼び出します
let translateProjection e =
    match e with
    | Lambda(var1, NewTuple args) ->
        // すべてのタプルの要素を変換
        List.map (translatePropGet var1.Name) args
    | Lambda(var1, arg) ->
        // 本体の式が1つだけです
        [translatePropGet var1.Name arg]
    | _ -> failwith "ラムダ式を指定する必要があります"
```

さてこれですべての定義が終わったので、実際の動作を確認できるようになりました。
先ほど作成したサンプルのクエリ`q`に対して`translateQuery`を呼び出してみましょう：

```fsharp
q |> translateQuery
```

実行してみるとクエリを表すために必要な情報がすべてそろった、
いい感じの`Query`の値が返されている事が確認できます。
`where`句を1つ持ったクエリの場合、以下のような結果になります：

```fsharp
{ Source = "People"
  Where =
    [ { Property = "Age"
        Operator = "GreaterThan"
        Constant = 10 }]
  Select = Some SelectCount }
```

## まとめ

この記事ではたとえばAmazon DynamoDBのようなデータソースに対して
クエリを発行できるような、独自のクエリコンピュテーションを
実装する方法を紹介しました。
これはF#で出来る方法の中でも、一番簡単な方法というわけではありませんが、
LINQ式ツリーを変換するコードを書く場合と比較すればそれほど悪くも無いでしょう。
ここではアクティブパターンとパターンマッチの言語機能が
きわめて重要な役割を果たしています！

なおこの記事ではクエリを評価する部分の説明をしていません。
そのためには`Expr<'T>`を引数にとって、
(既に少し説明したように)式から`Query`を展開し、
(これがまさに必要なところですが)実際にデータベースへリクエストを送信して
`'T`型の値を生成するような`Run`操作を実装することになります。
この部分を抜きにしても、今回の最小限のサンプルで重要なポイントをおおむね
説明できたのではないかと思います！

[link1]: http://tomasp.net/blog/2015/query-translation/index.html
[link2]: http://www.fsharpworks.com/workshops/finance.html
[link3]: https://twitter.com/stuart_j_davies
[link4]: https://github.com/stuartjdavies/FSharp.Cloud.AWS/blob/master/FSharp.Cloud.AWS/DynamoDB.fs
