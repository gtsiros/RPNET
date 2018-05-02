<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SimpleUI
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.outp = New System.Windows.Forms.TextBox()
        Me.inp = New System.Windows.Forms.TextBox()
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        Me.SuspendLayout()
        '
        'outp
        '
        Me.outp.Dock = System.Windows.Forms.DockStyle.Fill
        Me.outp.Location = New System.Drawing.Point(0, 0)
        Me.outp.Margin = New System.Windows.Forms.Padding(4)
        Me.outp.Multiline = True
        Me.outp.Name = "outp"
        Me.outp.ReadOnly = True
        Me.outp.Size = New System.Drawing.Size(867, 264)
        Me.outp.TabIndex = 2
        Me.outp.WordWrap = False
        '
        'inp
        '
        Me.inp.AcceptsTab = True
        Me.inp.Dock = System.Windows.Forms.DockStyle.Fill
        Me.inp.Location = New System.Drawing.Point(0, 0)
        Me.inp.Margin = New System.Windows.Forms.Padding(4)
        Me.inp.Multiline = True
        Me.inp.Name = "inp"
        Me.inp.Size = New System.Drawing.Size(867, 262)
        Me.inp.TabIndex = 1
        Me.inp.WordWrap = False
        '
        'SplitContainer1
        '
        Me.SplitContainer1.BackColor = System.Drawing.SystemColors.Control
        Me.SplitContainer1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitContainer1.Location = New System.Drawing.Point(0, 0)
        Me.SplitContainer1.Margin = New System.Windows.Forms.Padding(4)
        Me.SplitContainer1.Name = "SplitContainer1"
        Me.SplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal
        '
        'SplitContainer1.Panel1
        '
        Me.SplitContainer1.Panel1.Controls.Add(Me.outp)
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Controls.Add(Me.inp)
        Me.SplitContainer1.Size = New System.Drawing.Size(867, 535)
        Me.SplitContainer1.SplitterDistance = 264
        Me.SplitContainer1.SplitterWidth = 9
        Me.SplitContainer1.TabIndex = 3
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 19.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(867, 535)
        Me.Controls.Add(Me.SplitContainer1)
        Me.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.KeyPreview = True
        Me.Margin = New System.Windows.Forms.Padding(4)
        Me.Name = "Form1"
        Me.Text = "Form1"
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel1.PerformLayout()
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        Me.SplitContainer1.Panel2.PerformLayout()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer1.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents outp As TextBox
    Friend WithEvents inp As TextBox
    Friend WithEvents SplitContainer1 As SplitContainer
End Class
