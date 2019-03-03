Imports Windows.Data.Pdf
Imports Windows.Graphics.Imaging
Imports Windows.Media.Ocr
Imports Windows.Storage
Imports Windows.Storage.Streams

Namespace Global.PDFSupport

    Public Class PDFScanner

        Public Shared Async Function ScanPDFForCaloriesAsync(pdfFile As StorageFile) As Task(Of Integer)

            Dim calories As Integer = 0

            Try
                If pdfFile IsNot Nothing Then
                    'Load pdf from file. 
                    Dim pdfDoc As PdfDocument = Await PdfDocument.LoadFromFileAsync(pdfFile)
                    Return Await ScanPDFForCaloriesAsync(pdfDoc)
                End If
            Catch ex As Exception
            End Try

            Return calories

        End Function

        Public Shared Async Function ScanPDFForCaloriesAsync(pdfDoc As PdfDocument) As Task(Of Integer)

            Dim calories As Integer = 0

            Try
                ' Get page count from pdf document 
                Dim pageCount As Integer = pdfDoc.PageCount
                For i = 0 To pageCount - 1
                    Using pdfPage = pdfDoc.GetPage(i)
                        Dim stream As New InMemoryRandomAccessStream()
                        ' Default Is actual size. Render pdf page to stream 
                        Await pdfPage.RenderToStreamAsync(stream, New PdfPageRenderOptions With {.DestinationHeight = 2400, .DestinationWidth = 2400})
                        ' Create bitmapImage for Image source 
                        Dim bitmap As New BitmapImage()
                        'Set stream as bitmapImage's source 
                        Await bitmap.SetSourceAsync(stream)
                        'New OcrEngine with default language 
                        Dim ocrEngine As OcrEngine = OcrEngine.TryCreateFromUserProfileLanguages()
                        Dim decoder As BitmapDecoder = Await BitmapDecoder.CreateAsync(stream)
                        Dim SoftwareBitmap As SoftwareBitmap = Await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied)
                        ' Get recognition result 
                        Dim result As OcrResult = Await ocrEngine.RecognizeAsync(SoftwareBitmap)
                        ' Add to result list 
                        'For j = 0 To result.Text.Length() - 1
                        '    Dim c = AscW(result.Text(j))
                        '    Dim v = result.Text(j)
                        '    If Char.IsWhiteSpace(v) Then
                        '        Dim x = 0
                        '    Else
                        '        Dim x = 1
                        '    End If
                        'Next
                        Dim intVal As Integer = -1
                        Dim tokens = result.Text.Split(New [Char]() {" "c, CChar(vbTab), CChar(vbCrLf)})
                        For Each token In tokens
                            If token.StartsWith("kcal") AndAlso intVal > 0 Then
                                calories = intVal
                            Else
                                Integer.TryParse(token, intVal)
                            End If
                        Next
                    End Using
                Next
            Catch ex As Exception
            End Try

            Return calories

        End Function

    End Class

End Namespace
