(**
# Tutorial: Functional Reactive Programming in F# and WPF を試す

ここでは Stephen Elliott 氏によって書かれた
[Tutorial: Functional Reactive Programming in F# and WPF](http://steellworks.blogspot.jp/2014/03/tutorial-functional-reactive.html)
をfsi.exeで実行可能にしつつ、
[FSharp.Formatting](http://tpetricek.github.io/FSharp.Formatting/)
で文書化したりします。

実行方法：

    fsi.exe [-d:VERSIONX] --exec part1.fsx

`VERSIONX` の `X` には `1` から `5` までの数字が入ります。

例：

    fsi.exe -d:VERSION5 --exec part1.fsx

TODO: 後で書く
*)

#r "PresentationCore.dll"
#r "PresentationFramework.dll"
#r "System.Xaml.dll"
#r "WindowsBase.dll"

open System
open System.Windows
open System.Windows.Controls
open System.Xaml

let xamlString = """<?xml version="1.0" encoding="utf-8"?>
<Window
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Height="600" Width="800">

    <Canvas Name="Canvas" Background="White">
        <Rectangle Name="Rectangle" Width="100" Height="100" Fill="Black" RadiusY="10" RadiusX="10"/>
    </Canvas>
</Window>
"""

module version1 =
    let handle (canvas:Canvas) (rectangle:Shapes.Rectangle) =
        let mouse_down_handler (args : Input.MouseButtonEventArgs) =
            let mouse_position = args.GetPosition canvas
            Canvas.SetLeft(rectangle, mouse_position.X)
            Canvas.SetTop(rectangle, mouse_position.Y)
        let subscription = Observable.subscribe mouse_down_handler canvas.MouseDown
        subscription

module version2 =
    let handle (canvas:Canvas) (rectangle:Shapes.Rectangle) =
        let get_canvas_position (args : Input.MouseButtonEventArgs) =
            args.GetPosition canvas

        let set_rect_position (position : Point) =
            Canvas.SetLeft(rectangle, position.X)
            Canvas.SetTop(rectangle, position.Y)

        let subscription =
            Observable.subscribe
                set_rect_position
                (Observable.map get_canvas_position canvas.MouseDown)
        subscription

module version2_5 =
    let handle (canvas:Canvas) (rectangle:Shapes.Rectangle) =
        let get_canvas_position (args : Input.MouseButtonEventArgs) =
            args.GetPosition canvas

        let set_rect_position (position : Point) =
            Canvas.SetLeft(rectangle, position.X)
            Canvas.SetTop(rectangle, position.Y)

        let subscription =
            canvas.MouseDown
            |> Observable.map get_canvas_position
            |> Observable.subscribe set_rect_position
        subscription

module version3 =
    let handle (canvas:Canvas) (rectangle:Shapes.Rectangle) =
        let get_canvas_position (args : Input.MouseButtonEventArgs) =
            args.GetPosition canvas

        let set_rect_position (position : Point) =
            Canvas.SetLeft(rectangle, position.X)
            Canvas.SetTop(rectangle, position.Y)

        let is_left_click (args : Input.MouseButtonEventArgs) =
            args.ChangedButton = Input.MouseButton.Left

        let subscription =
            canvas.MouseDown
            |> Observable.filter is_left_click
            |> Observable.map get_canvas_position
            |> Observable.subscribe set_rect_position
        subscription

module version4 =
    /// ドラッグ操作の状態
    type DragState = { 
        /// 現在ドラッグ中？
        dragging : bool;

        /// ドラッグの現在位置
        position : Point }

    /// 現在ドラッグ中？
    let currently_dragging (state : DragState) : bool = state.dragging

    let get_drag_position (state : DragState) : Point = state.position

    /// ドラッグ操作の初期状態。
    let initial_state = { dragging=false; position=new Point() }

    /// 既存のDragStateからドラッグ中のフラグだけが異なる新しいDragStateを作る。
    let update_dragging (dragging : bool) (op : DragState) : DragState =
        { op with dragging=dragging }

    /// DragStateのPositionを更新する。ドラッグ操作中では無い場合には何もしない。
    let update_drag_pos (position : Point) (op : DragState) : DragState =
        if currently_dragging op
        then { op with position=position }
        else op

    /// DragStateによって起こりうる変更の種類。
    type DragChange = 
        /// ドラッグ開始。
        | StartDrag
        /// ドラッグ終了。
        | StopDrag 
        /// ドラッグの位置を更新。
        | UpdatePosition of Point

    /// DragChangeに応じてDragStateを更新して、新しいDragStateを返す。
    let update_drag_state (state : DragState) (change : DragChange) : DragState =
        match change with
        | StartDrag -> update_dragging true state
        | StopDrag -> update_dragging false state
        | UpdatePosition(pos) -> update_drag_pos pos state

    /// マウスイベントが左クリックによって発生したかどうか。
    let is_left_click (args : Input.MouseButtonEventArgs) : bool =
        args.ChangedButton = Input.MouseButton.Left

    /// マウスイベント時にIInputElementからカーソルの相対位置を計算する。
    let get_mouse_position (relative_to : IInputElement) (args : Input.MouseEventArgs) : Point =
        args.GetPosition relative_to

    let handle (canvas:Canvas) (rectangle:Shapes.Rectangle) =
        let set_rect_position (position : Point) =
            Canvas.SetLeft(rectangle, position.X)
            Canvas.SetTop(rectangle, position.Y)

        let get_canvas_position = get_mouse_position canvas
        let get_rectangle_position = get_mouse_position rectangle

        let is_left_click (args : Input.MouseButtonEventArgs) =
            args.ChangedButton = Input.MouseButton.Left

        let start_stream =
            rectangle.MouseDown 
            |> Observable.filter is_left_click
            |> Observable.map (fun _ -> StartDrag)
        
        let stop_stream =
            canvas.MouseUp 
            |> Observable.filter is_left_click
            |> Observable.map (fun _ -> StopDrag)
        
        let move_stream =
            canvas.MouseMove
            |> Observable.map (get_canvas_position >> UpdatePosition)

        let subscription =
            Observable.merge start_stream stop_stream |> Observable.merge move_stream
            |> Observable.scan update_drag_state initial_state
            |> Observable.filter currently_dragging
            |> Observable.map get_drag_position
            |> Observable.subscribe set_rect_position
        subscription

module version5 =
    /// ドラッグ操作の状態
    type DragState = { 
        /// 現在ドラッグ中？
        dragging : bool;

        /// ドラッグの現在位置
        position : Point; 

        /// ドラッグの開始位置からのオフセット
        offset : Point }

    /// 現在ドラッグ中？
    let currently_dragging (state : DragState) : bool = state.dragging

    /// ドラッグ操作の現在位置をドラッグ開始位置と相対位置から計算する。
    let get_drag_position (state : DragState) : Point = 
        let diff = state.position - state.offset
        new Point(diff.X, diff.Y)

    /// ドラッグ操作の初期状態。
    let initial_state = { dragging=false; position=new Point(); offset=new Point() }

    /// 既存のDragStateからドラッグ中のフラグだけが異なる新しいDragStateを作る。
    let update_dragging (dragging : bool) (op : DragState) : DragState =
        { op with dragging=dragging }

    /// DragStateのPositionを更新する。ドラッグ操作中では無い場合には何もしない。
    let update_drag_pos (position : Point) (op : DragState) : DragState =
        if currently_dragging op
        then { op with position=position }
        else op

    /// DragStateによって起こりうる変更の種類。
    type DragChange = 
        /// 特定のオフセットからのドラッグ開始。
        | StartDrag of Point 
        /// ドラッグ終了。
        | StopDrag 
        /// ドラッグの位置を更新。
        | UpdatePosition of Point

    /// DragChangeに応じてDragStateを更新して、新しいDragStateを返す。
    let update_drag_state (state : DragState) (change : DragChange) : DragState =
        match change with
        | StartDrag(offset) -> { state with dragging=true; offset=offset }
        | StopDrag -> update_dragging false state
        | UpdatePosition(pos) -> update_drag_pos pos state

    /// マウスイベントが左クリックによって発生したかどうか。
    let is_left_click (args : Input.MouseButtonEventArgs) : bool =
        args.ChangedButton = Input.MouseButton.Left

    /// マウスイベント時にIInputElementからカーソルの相対位置を計算する。
    let get_mouse_position (relative_to : IInputElement) (args : Input.MouseEventArgs) : Point =
        args.GetPosition relative_to

    let handle (canvas:Canvas) (rectangle:Shapes.Rectangle) =
        let set_rect_position (position : Point) =
            Canvas.SetLeft(rectangle, position.X)
            Canvas.SetTop(rectangle, position.Y)

        let get_canvas_position = get_mouse_position canvas
        let get_rectangle_position = get_mouse_position rectangle

        let is_left_click (args : Input.MouseButtonEventArgs) =
            args.ChangedButton = Input.MouseButton.Left

        let start_stream =
            rectangle.MouseDown 
            |> Observable.filter is_left_click
            |> Observable.map (get_rectangle_position >> StartDrag)
        
        let stop_stream =
            canvas.MouseUp 
            |> Observable.filter is_left_click
            |> Observable.map (fun _ -> StopDrag)
        
        let move_stream =
            canvas.MouseMove
            |> Observable.map (get_canvas_position >> UpdatePosition)

        let subscription =
            Observable.merge start_stream stop_stream |> Observable.merge move_stream
            |> Observable.scan update_drag_state initial_state
            |> Observable.filter currently_dragging
            |> Observable.map get_drag_position
            |> Observable.subscribe set_rect_position
        subscription

let loadMainWindowFromXaml() =
    XamlServices.Parse(xamlString) :?> Window

let startupAdd e =
    let window = loadMainWindowFromXaml()
    
    let windowCanvas = window.FindName("Canvas") :?> Canvas
    let windowRectangle = window.FindName("Rectangle") :?> Shapes.Rectangle

    let subscription =
        windowCanvas.MouseDown
        |> Observable.subscribe (fun e ->
#if VERSION2
            version2.handle windowCanvas windowRectangle |> ignore
#else
    #if VERSION3
            version3.handle windowCanvas windowRectangle |> ignore
    #else
        #if VERSION4
            version4.handle windowCanvas windowRectangle |> ignore
        #else
            #if VERSION5
            version5.handle windowCanvas windowRectangle |> ignore
            #else
            version1.handle windowCanvas windowRectangle |> ignore
            #endif
        #endif
    #endif
#endif
            )

    window.Show()

type App() as x =
    inherit Application()
    do
        x.Startup.Add <| startupAdd

let main() =
    let app = App()
    app.Run() |> ignore

[<STAThread>]
do
    main()
