Imports System.IO
Imports System.Drawing.Text
Imports System.Drawing.Drawing2D
Imports System.ComponentModel
Imports System.Windows.Forms.Control
Public Class Form1


    'instanzia una classe Windows.Media.Player con eventi
    'a scanso di equivoci, questo non ha a che fare col programma Windows Media Player
    WithEvents player As New Windows.Media.MediaPlayer


    Dim inPlay As Boolean
    Dim icona As Bitmap
    Dim SoundFile As New ArrayList From {"*.wav", "*.wma", "*.mpeg", "*.wmv", "*.mp3", "*.mp4", "*.mid", "*.midi", "*au", "*aif"}
    Dim inPlayGeneral As Boolean = False
    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        inPlayGeneral = True
        If ListBox1.SelectedIndex < 0 Then Exit Sub
        Dim file = ListBox1.Items(ListBox1.SelectedIndex).ToString
        If IO.File.Exists(file) Then

            Cursor = Cursors.WaitCursor

            'chiude il player prima di aprire un nuovo file
            If player.Source IsNot Nothing Then player.Close()

            Try
                'apre il file e ne impedisce l'immediata riproduzione

                player.Open(New Uri(file, UriKind.Relative))
                player.Stop()


                'Il programma usa la proprietà NaturalDuration che ritorna la durata del brano
                'ma non è immediatamente disponibile, questo loop aspetta che lo sia
                Do Until player.NaturalDuration.HasTimeSpan
                    Application.DoEvents()
                Loop

                ''se il file non ha audio è possibile sia un video senza sonoro
                ''questo genera un errore per uscire e chiudere il player
                'If Not player.HasAudio Then
                '    Throw New ApplicationException("Il file selezionato non ha un audio riproducibile.")
                'End If

                'rileva la durata del brano 
                Dim durata = player.NaturalDuration.TimeSpan
                'e la mostra
                Label8.Text = String.Format("Durata: {0}:{1}", durata.Minutes, durata.Seconds)

                'imposta il valore massimo della trackbar Ttime con la durata in secondi del brano
                Ttime.Maximum = CInt(player.NaturalDuration.TimeSpan.TotalSeconds)
                'Ttime.TickFrequency = Ttime.Maximum \ 10

                'settaggi iniziali per il player
                Tvolume_Scroll(Tvolume, Nothing)
                Tbalance_Scroll(Tbalance, Nothing)
                Tspeed_Scroll(Tspeed, Nothing)

                'se nel file è presente un video allora 
                'viene caricato il form Viever per la riproduzione


                'avvia l'esecuzione del brano audio o video
                player.Play()

                'aggiornamento conteggio brani
                Dim dirparent As String = IO.Path.GetDirectoryName(file)
                Dim nr = FileIO.FileSystem.GetFiles(dirparent, FileIO.SearchOption.SearchTopLevelOnly, "*.wav", "*.wma", "*.wmv", "*.mp3", "*.mp4", "*.mid", "*.midi", "*au", "*aif").Count
                Dim ar = dirparent.Split("\"c)
                Label6.Text = ListBox1.Items.Count.ToString & " brani /  " & nr.ToString & " in [ " & ar.Last.ToString & " ]"
                IsPlay = True
            Catch ex As Exception

                If player IsNot Nothing Then player.Close()
                Timer1.Enabled = False
                MsgBox(ex.Message)

            End Try

            Cursor = Cursors.Default

        End If

    End Sub
    Public playlista = False
    Private Sub Button3_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button3.Click
        'ferma il brano reimpostandolo dall'inizio
        inPlayGeneral = False
        player.Stop()
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        If My.Computer.Keyboard.ShiftKeyDown Then
            ListBox1.Items.Clear()

            For Each items In Directory.GetFiles(Environment.SpecialFolder.MyMusic)
                ListBox1.Items.Add(items)

            Next
        Else
            'se il tasto Control è premuto ...
            If ModifierKeys = Keys.Control Then
                'permette una ricerca ricorsiva entro la cartella selezionata
                'tutti i file supportati verranno importati nella ListBox
                Using fb As New FolderBrowserDialog
                    If fb.ShowDialog = DialogResult.OK Then

                        Label6.Visible = True
                        'aggiunge alla lista eliminando quelli presenti
                        AddItems(fb.SelectedPath, True)

                        'finita la ricerca mostra il numero di brani trovati
                        Label6.Text = ListBox1.Items.Count.ToString + " brani."

                        If ListBox1.SelectedIndex = -1 And ListBox1.Items.Count > 0 Then
                            ListBox1.SelectedIndex = 0
                            ListBox1.Select()
                        End If

                    End If
                End Using

            Else 'permette di importare uno o più brani

                Using ofd As New OpenFileDialog
                    ofd.Filter = "File musicali|*.ogg;*.wav;*.wma;*.mpeg;*.mp3;*.mid;*.midi;*.au;*.aif;*.m4a"
                    ofd.Multiselect = True
                    If ofd.ShowDialog = DialogResult.OK Then
                        Label6.Visible = True
                        'aggiunge alla lista eliminando quelli presenti
                        AddItems(ofd.FileNames, True)
                        inPlayGeneral = False
                        ListBox1.SelectedIndex = 0
                        ListBox1.Select()
                        Label6.Text = ListBox1.Items.Count.ToString + " brani."
                    End If
                End Using

            End If

        End If
        playlista = True
    End Sub
    Public Sub caricaFile(ByVal Dir As String)
        For Each line In System.IO.File.ReadAllLines(Dir)
            Try
                If File.Exists(line) Then
                    AddItems(line)

                End If

            Catch ex As Exception

            End Try

        Next
    End Sub
    Public Sub AddItems(ByVal item As String, ByVal delete As Boolean)

        If delete Then
            ListBox1.Items.Clear()
            player.Close()
        End If

        'ricerca in sottocartelle o intera unità
        If FileIO.FileSystem.DirectoryExists(item) Then
            Btnstop.Visible = True
            ProgressBar1.Visible = True
            Label7.Text = ""
            Label7.Visible = True
            stopsearch = False
            CercaSubDir(item, ListBox1, "*.avi", "*.wav", "*.wma", "*.ogg", "*.wmv", "*.mpeg", "*.ogg", "*.mp3", "*.mp4", "*.mid", "*.midi", "*.au", "*.aif")
            Btnstop.Visible = False
            ProgressBar1.Visible = False
            Label7.Visible = False
        Else
            AddItems(item)
        End If


    End Sub
#Region "Funzione di ricerca brani ricorsiva nella cartella selezionata"

    Dim stopsearch As Boolean
    Sub CercaSubDir(ByVal path As String, ByVal lst As ListBox, ByVal ParamArray ext() As String)

        For Each dirName As String In IO.Directory.GetDirectories(path)
            If stopsearch Then Exit Sub
            CercaSubDir(dirName, lst, ext)
            Dim sound = FileIO.FileSystem.GetFiles(dirName, FileIO.SearchOption.SearchTopLevelOnly, ext).ToArray
            If sound.Length > 0 Then
                lst.Items.AddRange(sound)
                'formatta la stringa di avanzamento mostrando il numero di brani trovati nella cartella e il suo indice nella lista
                Label6.Text = String.Format("{1}° {0}  :{2}", sound.Length, lst.Items.Count, dirName.Substring(dirName.LastIndexOf("\")))
            End If
            'cartella corrente
            Label7.Text = dirName
            Application.DoEvents()
        Next

    End Sub
    Private Sub BtnStop_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Btnstop.Click
        stopsearch = True
    End Sub

#End Region

    Sub AddItems(ByVal items() As String, ByVal delete As Boolean)

        'aggiunge una lista di brabi nella ListBox ed eventualmente elimina quelli presenti chiudendo il player

        If delete Then
            ListBox1.Items.Clear()
            player.Close()
        End If

        For Each item In items
            AddItems(item)
        Next

    End Sub

    Sub AddItems(ByVal item As String)
        ListBox1.Items.Add(item)
    End Sub
    Public IsPlay As Boolean = False
    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button4.Click

        'a ogni pressione del tasto il player viene messo in pausa oppure riavviato
        'il font dei pulsanti è windings
        If inPlay Then
            player.Pause()
            Button4.Image = My.Resources.play_pause
            inPlay = False
            IsPlay = False
        Else
            If player.Source IsNot Nothing Then player.Play()
            Button4.Image = My.Resources.pause
            inPlay = True
            IsPlay = True
        End If
    End Sub

    Private Sub player_MediaOpened(ByVal sender As Object, ByVal e As System.EventArgs) Handles player.MediaOpened
        'questo evento si verifica all'avvio del brano

        'l'evento viene usato per avviare il timer che rileva la posizione nel brano
        'e azzerare la trackbar relativa alla posizione
        inPlay = True
        IsPlay = True
        Ttime.Value = 0
        Timer1.Interval = 500
        Timer1.Enabled = True
        ListBox1.Refresh()

    End Sub

    Private Sub player_MediaEnded(ByVal sender As Object, ByVal e As System.EventArgs) Handles player.MediaEnded
        'questo evento si verifica al termine del brano

        'rileva il tipo di ripetizione da effettuare sulla playlist
        Select Case CheckLoop.CheckState

            Case CheckState.Checked 'ripete il brano selezionato riportando a 0 il player
                player.Position = New TimeSpan(0)

            Case CheckState.Indeterminate 'ripete tutta la playlist
                Dim index = ListBox1.SelectedIndex

                'se è l'ultimo item ad essere selezionato si riposiziona sul primo
                'altrimenti avanza di uno nella lista
                If index = ListBox1.Items.Count - 1 Then
                    ListBox1.SelectedIndex = 0
                    Button2.PerformClick()
                Else
                    ListBox1.SelectedIndex = index + 1
                    Button2.PerformClick()
                End If

            Case Else 'nessuna ripetizione, ferma il timer e riporta a 0 il player

                player.Stop()
                player.Position = New TimeSpan(0)
                Timer1.Enabled = False
        End Select

    End Sub

    Private Sub Timer1_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles Timer1.Tick

        Dim posizione = player.Position
        'viene aggiornata la label con la posizione istantanea
        Label2.Text = String.Format("Posizione: {0}:{1}", posizione.Minutes, posizione.Seconds)
        'e anche la trackbar relativa
        Ttime.Value = CInt(player.Position.TotalSeconds)
    End Sub

    Private Sub Tvolume_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Tvolume.Scroll
        'regola il volume del player
        player.Volume = TryCast(sender, TrackBar).Value / 100
    End Sub

    Private Sub Tbalance_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Tbalance.Scroll
        'regola il bilanciamento del player
        player.Balance = TryCast(sender, TrackBar).Value / 5
    End Sub

    Private Sub Tspeed_Scroll(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Tspeed.Scroll
        'regola la velocità di esecuzione dl brano
        player.SpeedRatio = TryCast(sender, TrackBar).Value / 10
    End Sub

    Private Sub Form1_FormClosed(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles Me.FormClosed
        'resetta il player prima di chiudere il programma
        Try
            stopsearch = True
            player.Close()
            player = Nothing
            Me.Close()
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Form1_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles Me.Paint
        If icona IsNot Nothing Then
            'disegna l'icona associata al formato del brano selezionato
            e.Graphics.DrawImage(icona, New Rectangle(10, 240, 32, 32))
            'disegna il titolo del brano con un effetto ombra sottostante
            ShadowText(e.Graphics, icona.Tag.ToString, New Font("broadway bt", 10, FontStyle.Bold Or FontStyle.Italic), Color.RoyalBlue, New Rectangle(45, 240, 450, 46), 2, New PointF(2, 2.5), New Padding(0, 0, 0, 0))
        End If
    End Sub
    Public Sub ShadowText(ByVal g As Graphics, ByVal text As String, ByVal font As Font, ByVal Colore As Color, ByVal rect As Rectangle, ByVal _Blur As Single, ByVal _offset As PointF, ByVal _Padding As Padding)

        'crea una bitmap con il testo sfocato
        'la sfocatura avviene disegnando un testo con antialias in una bitmap
        'più piccola di una certa entità e poi ridisegnata ingrandendola della stessa entità
        Using bm As Bitmap = New Bitmap(CInt(rect.Width / _Blur), CInt(rect.Height / _Blur)), _sf As New StringFormat
            _sf.LineAlignment = StringAlignment.Center
            _sf.Alignment = StringAlignment.Near

            Using gBlur As Graphics = Graphics.FromImage(bm)
                gBlur.TextRenderingHint = TextRenderingHint.AntiAlias
                'questa matrice scala e sposta l'ombra rispetto all'originale
                Dim mx As Matrix = New Matrix(1 / _Blur, 0, 0, 1 / _Blur, _offset.X, _offset.Y)
                gBlur.Transform = mx

                'disegna l'ombra
                gBlur.DrawString(text, font, New SolidBrush(Color.FromArgb(128, Colore)), New Rectangle(0, 0,
                   CInt(rect.Width - (_offset.X * _Blur) - _Padding.Horizontal),
                   CInt(rect.Height - (_offset.Y) * _Blur) - _Padding.Vertical), _sf)
            End Using

            rect.Offset(_Padding.Left, _Padding.Top)

            'alta qualità per il disegno
            g.InterpolationMode = InterpolationMode.HighQualityBicubic
            g.TextRenderingHint = TextRenderingHint.AntiAlias

            'disegna la bitmap con il testo sfocato nell'area client
            g.DrawImage(bm, rect, 0, 0, bm.Width, bm.Height, GraphicsUnit.Pixel)

            'disegna il testo pieno sopra quello sfocato
            rect.Width = CInt(rect.Width - (_offset.X * _Blur) - _Padding.Horizontal)
            rect.Height = CInt(rect.Height - (_offset.Y * _Blur) - _Padding.Vertical)
            g.DrawString(text, font, New SolidBrush(Colore), rect, _sf)
        End Using

    End Sub

    Private Sub ListBox1_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles ListBox1.DoubleClick
        Button2.PerformClick()
    End Sub

    Private Sub ListBox1_DrawItem(ByVal sender As Object, ByVal e As System.Windows.Forms.DrawItemEventArgs) Handles ListBox1.DrawItem

        If e.Index > -1 Then
            Dim rc = e.Bounds
            Dim path = ListBox1.Items(e.Index).ToString
            Dim txt = IO.Path.GetFileName(path)

            Using sf As New StringFormat
                sf.LineAlignment = StringAlignment.Center
                sf.Alignment = StringAlignment.Near

                If (e.State And DrawItemState.Focus) = 0 Or (e.State And DrawItemState.Selected) = 0 Then
                    If player.Source IsNot Nothing AndAlso path = player.Source.ToString Then
                        'da arancio scuro 244; 113; 34 a chiaro  DarkOrange

                        e.Graphics.FillRectangle(New LinearGradientBrush(rc, Color.FromArgb(51, 153, 255), Color.White, 45), rc)
                        e.Graphics.DrawString(txt, New Font(e.Font.Name, e.Font.Size, FontStyle.Bold), SystemBrushes.WindowText, rc, sf)
                    Else
                        e.Graphics.FillRectangle(New SolidBrush(Color.White), e.Bounds)
                        e.Graphics.DrawString(txt, e.Font, SystemBrushes.WindowText, rc, sf)
                    End If
                Else
                    If player.Source IsNot Nothing AndAlso path = player.Source.ToString Then
                        e.Graphics.FillRectangle(New LinearGradientBrush(rc, Color.FromArgb(51, 153, 255), Color.White, 45), rc)
                        e.Graphics.DrawString(txt, New Font(e.Font.Name, e.Font.Size, FontStyle.Bold), SystemBrushes.HighlightText, rc, sf)
                    Else
                        e.Graphics.FillRectangle(New LinearGradientBrush(rc, Color.FromArgb(51, 153, 255), Color.White, 45), e.Bounds)
                        e.Graphics.DrawString(txt, e.Font, New SolidBrush(Color.Black), rc, sf)
                    End If
                End If
            End Using

        End If

    End Sub

    Private Sub ListBox1_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles ListBox1.KeyUp
        If e.KeyValue = Keys.Enter Then
            Button2.PerformClick()
        End If
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ListBox1.SelectedIndexChanged

        'il programma supporta formati diversi e i formati possono avere icone diverse
        'selezionando un brano qui viene estratta l'icona associata al formato
        'e quindi disegnata nel form accompagnata dal titolo del brano
        If ListBox1.SelectedIndex > -1 Then
            Dim ret = ListBox1.Items(ListBox1.SelectedIndex).ToString
            If FileIO.FileSystem.FileExists(ret) Then
                'estrae l'icona e la memorizza come bitmap
                icona = Icon.ExtractAssociatedIcon(ret).ToBitmap
                'nome del brano da disegnare insieme all'icona
                icona.Tag = IO.Path.GetFileName(ret)
                Invalidate()
            End If
        End If
    End Sub

    Dim itemindex As Integer
    Private Sub ListBox1_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles ListBox1.MouseDown
        'rileva l'indice selezionato tramite la posizione del puntatore
        itemindex = ListBox1.IndexFromPoint(e.X, e.Y)
    End Sub

    Private Sub ListBox1_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles ListBox1.MouseMove

        'gli item della lista possono essere trascinati in altre posizioni tramite il mouse

        Dim indice = ListBox1.IndexFromPoint(e.X, e.Y)
        'se è un indice valido gli item sono spostati col tasto sonostro
        If e.Button = MouseButtons.Left And itemindex <> indice And indice > -1 Then
            'lo spostamento avviene tramite uno scambio tra il vecchio "itemindex" e il nuovo "indice"
            swap(ListBox1.Items(indice), ListBox1.Items(itemindex))
            itemindex = indice
        End If

    End Sub
    ''' <summary>
    ''' funzione di scambio valida per ogni tipo di oggetto
    ''' </summary>    
    ''' <remarks>da notare che gli oggetti sono passato per riferimento</remarks>
    Sub swap(Of T)(ByRef A As T, ByRef B As T)
        Dim tmp = A
        A = B
        B = tmp
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim stringa As String() = Environment.GetCommandLineArgs
        Try
            Dim balu As String = My.Application.CommandLineArgs(0)
            AddItems(balu, True)
            ListBox1.SelectedIndex = 0
            ListBox1.Select()
            Label6.Text = ListBox1.Items.Count.ToString + " brani."
            Button2.PerformClick()
        Catch ex As Exception

        End Try

    End Sub

    Private Sub ButtonHide_Click(sender As Object, e As EventArgs)
        Me.Hide()
    End Sub

    Private Sub ListBox1_KeyPress(sender As Object, e As KeyPressEventArgs) Handles ListBox1.KeyPress
        If e.KeyChar = ChrW(Keys.Space) Then
            If inPlayGeneral = False Then
                Button2.PerformClick()
            Else
                Button4.PerformClick()
            End If

        End If
    End Sub

    Private Sub Form1_KeyPress(sender As Object, e As KeyPressEventArgs) Handles Me.KeyPress
        If e.KeyChar = ChrW(Keys.Space) Then
            If inPlayGeneral = False Then
                Button2.PerformClick()
            Else
                Button4.PerformClick()
            End If

        End If
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        Dim ofd As New OpenFileDialog
        ofd.Filter = "File Playlist|*.jpl"
        If ofd.ShowDialog = DialogResult.OK Then
            caricaFile(ofd.FileName)
        End If
    End Sub
End Class

