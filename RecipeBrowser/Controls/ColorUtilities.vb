Imports Windows.UI

Namespace Global.RecipeBrowser.Controls

    Public Class ColorUtilities

        Private Shared ReadOnly _designColors() = {Color.FromArgb(&HFF, &HFF, &HFF, &HFF),
                                Color.FromArgb(&HFF, &H0, &H0, &H0),
                                Color.FromArgb(&HFF, &HE7, &HE6, &HE6),
                                Color.FromArgb(&HFF, &H44, &H45, &H6A),
                                Color.FromArgb(&HFF, &H44, &H72, &HC4),
                                Color.FromArgb(&HFF, &HED, &H7D, &H31),
                                Color.FromArgb(&HFF, &HA5, &HA5, &HA5),
                                Color.FromArgb(&HFF, &HFF, &HC0, &H0),
                                Color.FromArgb(&HFF, &H5B, &H9B, &HD5),
                                Color.FromArgb(&HFF, &H70, &HAD, &H47),
                                Color.FromArgb(&HFF, &HE5, &HE5, &HE5),
                                Color.FromArgb(&HFF, &HE5, &HE5, &HE5),
                                Color.FromArgb(&HFF, &HD0, &HCE, &HCE),
                                Color.FromArgb(&HFF, &HD6, &HDC, &HE4),
                                Color.FromArgb(&HFF, &HD9, &HE2, &HF3),
                                Color.FromArgb(&HFF, &HFB, &HE5, &HD5),
                                Color.FromArgb(&HFF, &HED, &HED, &HED),
                                Color.FromArgb(&HFF, &HFF, &HF2, &HCC),
                                Color.FromArgb(&HFF, &HDE, &HEB, &HF6),
                                Color.FromArgb(&HFF, &HE2, &HEF, &HD9),
                                Color.FromArgb(&HFF, &HBF, &HBF, &HBF),
                                Color.FromArgb(&HFF, &HBF, &HBF, &HBF),
                                Color.FromArgb(&HFF, &HAE, &HAB, &HAB),
                                Color.FromArgb(&HFF, &HAB, &HB9, &HCA),
                                Color.FromArgb(&HFF, &HB4, &HC6, &HE7),
                                Color.FromArgb(&HFF, &HF7, &HCB, &HAC),
                                Color.FromArgb(&HFF, &HDB, &HDB, &HDB),
                                Color.FromArgb(&HFF, &HFE, &HE5, &H99),
                                Color.FromArgb(&HFF, &HBD, &HD7, &HEE),
                                Color.FromArgb(&HFF, &HC5, &HE0, &HB3),
                                Color.FromArgb(&HFF, &H7F, &H7F, &H7F),
                                Color.FromArgb(&HFF, &H7F, &H7F, &H7F),
                                Color.FromArgb(&HFF, &H75, &H70, &H70),
                                Color.FromArgb(&HFF, &H84, &H96, &HB0),
                                Color.FromArgb(&HFF, &H8E, &HAA, &HDB),
                                Color.FromArgb(&HFF, &HF4, &HB1, &H83),
                                Color.FromArgb(&HFF, &HC9, &HC9, &HC9),
                                Color.FromArgb(&HFF, &HFF, &HD9, &H65),
                                Color.FromArgb(&HFF, &H9C, &HC3, &HE5),
                                Color.FromArgb(&HFF, &HA8, &HD0, &H8D),
                                Color.FromArgb(&HFF, &H3F, &H3F, &H3F),
                                Color.FromArgb(&HFF, &H3F, &H3F, &H3F),
                                Color.FromArgb(&HFF, &H3A, &H38, &H38),
                                Color.FromArgb(&HFF, &H2F, &H54, &H96),
                                Color.FromArgb(&HFF, &H2F, &H54, &H96),
                                Color.FromArgb(&HFF, &HC5, &H5A, &H11),
                                Color.FromArgb(&HFF, &H7B, &H7B, &H7B),
                                Color.FromArgb(&HFF, &HBF, &H90, &H0),
                                Color.FromArgb(&HFF, &H2E, &H75, &HB5),
                                Color.FromArgb(&HFF, &H53, &H81, &H35),
                                Color.FromArgb(&HFF, &H19, &H19, &H19),
                                Color.FromArgb(&HFF, &H19, &H19, &H19),
                                Color.FromArgb(&HFF, &H17, &H16, &H16),
                                Color.FromArgb(&HFF, &H22, &H2A, &H35),
                                Color.FromArgb(&HFF, &H1F, &H38, &H64),
                                Color.FromArgb(&HFF, &H83, &H3C, &HB),
                                Color.FromArgb(&HFF, &H52, &H52, &H52),
                                Color.FromArgb(&HFF, &H7F, &H60, &H0),
                                Color.FromArgb(&HFF, &H1E, &H4E, &H79),
                                Color.FromArgb(&HFF, &H37, &H56, &H23)}

        Public Shared ReadOnly Property DesignColors
            Get
                Return _designColors
            End Get
        End Property

        Private Shared ReadOnly _standardColors() = {Color.FromArgb(&HFF, &HC0, &H0, &H0),
                                Color.FromArgb(&HFF, &HFF, &H0, &H0),
                                Color.FromArgb(&HFF, &HFF, &HC0, &H0),
                                Color.FromArgb(&HFF, &HFF, &HFF, &H0),
                                Color.FromArgb(&HFF, &H92, &HD0, &H50),
                                Color.FromArgb(&HFF, &H0, &HB0, &H50),
                                Color.FromArgb(&HFF, &H0, &HB0, &HF0),
                                Color.FromArgb(&HFF, &H0, &H70, &HC0),
                                Color.FromArgb(&HFF, &H0, &H20, &H60),
                                Color.FromArgb(&HFF, &H70, &H30, &HA0)}

        Public Shared ReadOnly Property StandardColors
            Get
                Return _standardColors
            End Get
        End Property

        Public Shared Function GetForegroundColor(background As Color) As Color
            Dim r As Double = background.R
            Dim g As Double = background.G
            Dim b As Double = background.B
            Dim s = r + g + b
            If r + g + b > 3 * 255 / 2 Then
                Return Colors.Black
            Else
                Return Colors.White
            End If
        End Function

    End Class

End Namespace
