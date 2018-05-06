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
        Me.stk = New System.Windows.Forms.TextBox()
        Me.inp = New System.Windows.Forms.TextBox()
        Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
        Me.SplitContainer2 = New System.Windows.Forms.SplitContainer()
        Me.outp = New System.Windows.Forms.TextBox()
        Me.SplitContainer3 = New System.Windows.Forms.SplitContainer()
        Me.hist = New System.Windows.Forms.TextBox()
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer1.Panel1.SuspendLayout()
        Me.SplitContainer1.Panel2.SuspendLayout()
        Me.SplitContainer1.SuspendLayout()
        CType(Me.SplitContainer2, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer2.Panel1.SuspendLayout()
        Me.SplitContainer2.Panel2.SuspendLayout()
        Me.SplitContainer2.SuspendLayout()
        CType(Me.SplitContainer3, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SplitContainer3.Panel1.SuspendLayout()
        Me.SplitContainer3.Panel2.SuspendLayout()
        Me.SplitContainer3.SuspendLayout()
        Me.SuspendLayout()
        '
        'stk
        '
        Me.stk.Dock = System.Windows.Forms.DockStyle.Fill
        Me.stk.Location = New System.Drawing.Point(0, 0)
        Me.stk.Margin = New System.Windows.Forms.Padding(4)
        Me.stk.Multiline = True
        Me.stk.Name = "stk"
        Me.stk.ReadOnly = True
        Me.stk.Size = New System.Drawing.Size(394, 456)
        Me.stk.TabIndex = 2
        Me.stk.WordWrap = False
        '
        'inp
        '
        Me.inp.AcceptsTab = True
        Me.inp.Dock = System.Windows.Forms.DockStyle.Fill
        Me.inp.Location = New System.Drawing.Point(0, 0)
        Me.inp.Margin = New System.Windows.Forms.Padding(4)
        Me.inp.Multiline = True
        Me.inp.Name = "inp"
        Me.inp.Size = New System.Drawing.Size(394, 460)
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
        Me.SplitContainer1.Panel1.Controls.Add(Me.SplitContainer2)
        '
        'SplitContainer1.Panel2
        '
        Me.SplitContainer1.Panel2.Controls.Add(Me.SplitContainer3)
        Me.SplitContainer1.Size = New System.Drawing.Size(1182, 925)
        Me.SplitContainer1.SplitterDistance = 456
        Me.SplitContainer1.SplitterWidth = 9
        Me.SplitContainer1.TabIndex = 3
        '
        'SplitContainer2
        '
        Me.SplitContainer2.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitContainer2.Location = New System.Drawing.Point(0, 0)
        Me.SplitContainer2.Name = "SplitContainer2"
        '
        'SplitContainer2.Panel1
        '
        Me.SplitContainer2.Panel1.Controls.Add(Me.stk)
        '
        'SplitContainer2.Panel2
        '
        Me.SplitContainer2.Panel2.Controls.Add(Me.outp)
        Me.SplitContainer2.Size = New System.Drawing.Size(1182, 456)
        Me.SplitContainer2.SplitterDistance = 394
        Me.SplitContainer2.TabIndex = 3
        '
        'outp
        '
        Me.outp.Dock = System.Windows.Forms.DockStyle.Fill
        Me.outp.Location = New System.Drawing.Point(0, 0)
        Me.outp.Multiline = True
        Me.outp.Name = "outp"
        Me.outp.ReadOnly = True
        Me.outp.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.outp.Size = New System.Drawing.Size(784, 456)
        Me.outp.TabIndex = 0
        Me.outp.WordWrap = False
        '
        'SplitContainer3
        '
        Me.SplitContainer3.Dock = System.Windows.Forms.DockStyle.Fill
        Me.SplitContainer3.Location = New System.Drawing.Point(0, 0)
        Me.SplitContainer3.Name = "SplitContainer3"
        '
        'SplitContainer3.Panel1
        '
        Me.SplitContainer3.Panel1.Controls.Add(Me.inp)
        '
        'SplitContainer3.Panel2
        '
        Me.SplitContainer3.Panel2.Controls.Add(Me.hist)
        Me.SplitContainer3.Size = New System.Drawing.Size(1182, 460)
        Me.SplitContainer3.SplitterDistance = 394
        Me.SplitContainer3.TabIndex = 2
        '
        'hist
        '
        Me.hist.Dock = System.Windows.Forms.DockStyle.Fill
        Me.hist.Location = New System.Drawing.Point(0, 0)
        Me.hist.Multiline = True
        Me.hist.Name = "hist"
        Me.hist.ReadOnly = True
        Me.hist.Size = New System.Drawing.Size(784, 460)
        Me.hist.TabIndex = 0
        Me.hist.WordWrap = False
        '
        'SimpleUI
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 19.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1182, 925)
        Me.Controls.Add(Me.SplitContainer1)
        Me.Font = New System.Drawing.Font("Consolas", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.KeyPreview = True
        Me.Margin = New System.Windows.Forms.Padding(4)
        Me.Name = "SimpleUI"
        Me.Text = "Form1"
        Me.SplitContainer1.Panel1.ResumeLayout(False)
        Me.SplitContainer1.Panel2.ResumeLayout(False)
        CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer1.ResumeLayout(False)
        Me.SplitContainer2.Panel1.ResumeLayout(False)
        Me.SplitContainer2.Panel1.PerformLayout()
        Me.SplitContainer2.Panel2.ResumeLayout(False)
        Me.SplitContainer2.Panel2.PerformLayout()
        CType(Me.SplitContainer2, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer2.ResumeLayout(False)
        Me.SplitContainer3.Panel1.ResumeLayout(False)
        Me.SplitContainer3.Panel1.PerformLayout()
        Me.SplitContainer3.Panel2.ResumeLayout(False)
        Me.SplitContainer3.Panel2.PerformLayout()
        CType(Me.SplitContainer3, System.ComponentModel.ISupportInitialize).EndInit()
        Me.SplitContainer3.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents stk As TextBox
    Friend WithEvents inp As TextBox
    Friend WithEvents SplitContainer1 As SplitContainer
    Friend WithEvents SplitContainer2 As SplitContainer
    Friend WithEvents outp As TextBox
    Friend WithEvents SplitContainer3 As SplitContainer
    Friend WithEvents hist As TextBox
End Class
