<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
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
		Me.tcmd = New System.Windows.Forms.TextBox()
		Me.tstk = New System.Windows.Forms.TextBox()
		Me.tinf = New System.Windows.Forms.TextBox()
		Me.SplitContainer1 = New System.Windows.Forms.SplitContainer()
		Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
		Me.ToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
		Me.HistoryToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
		CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).BeginInit()
		Me.SplitContainer1.Panel1.SuspendLayout()
		Me.SplitContainer1.Panel2.SuspendLayout()
		Me.SplitContainer1.SuspendLayout()
		Me.MenuStrip1.SuspendLayout()
		Me.SuspendLayout()
		'
		'tcmd
		'
		Me.tcmd.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.tcmd.Font = New System.Drawing.Font("GohuFont", 8.0!)
		Me.tcmd.Location = New System.Drawing.Point(3, 3)
		Me.tcmd.Multiline = True
		Me.tcmd.Name = "tcmd"
		Me.tcmd.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
		Me.tcmd.Size = New System.Drawing.Size(768, 255)
		Me.tcmd.TabIndex = 0
		'
		'tstk
		'
		Me.tstk.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.tstk.Font = New System.Drawing.Font("GohuFont", 8.0!)
		Me.tstk.Location = New System.Drawing.Point(3, 29)
		Me.tstk.Multiline = True
		Me.tstk.Name = "tstk"
		Me.tstk.ReadOnly = True
		Me.tstk.ScrollBars = System.Windows.Forms.ScrollBars.Both
		Me.tstk.Size = New System.Drawing.Size(768, 232)
		Me.tstk.TabIndex = 1
		Me.tstk.TabStop = False
		'
		'tinf
		'
		Me.tinf.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.tinf.Font = New System.Drawing.Font("GohuFont", 8.0!)
		Me.tinf.Location = New System.Drawing.Point(3, 3)
		Me.tinf.Name = "tinf"
		Me.tinf.ReadOnly = True
		Me.tinf.Size = New System.Drawing.Size(768, 20)
		Me.tinf.TabIndex = 2
		Me.tinf.TabStop = False
		'
		'SplitContainer1
		'
		Me.SplitContainer1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
			Or System.Windows.Forms.AnchorStyles.Left) _
			Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
		Me.SplitContainer1.Location = New System.Drawing.Point(12, 27)
		Me.SplitContainer1.Name = "SplitContainer1"
		Me.SplitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal
		'
		'SplitContainer1.Panel1
		'
		Me.SplitContainer1.Panel1.Controls.Add(Me.tinf)
		Me.SplitContainer1.Panel1.Controls.Add(Me.tstk)
		'
		'SplitContainer1.Panel2
		'
		Me.SplitContainer1.Panel2.Controls.Add(Me.tcmd)
		Me.SplitContainer1.Size = New System.Drawing.Size(774, 529)
		Me.SplitContainer1.SplitterDistance = 264
		Me.SplitContainer1.TabIndex = 3
		Me.SplitContainer1.TabStop = False
		'
		'MenuStrip1
		'
		Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripMenuItem1})
		Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
		Me.MenuStrip1.Name = "MenuStrip1"
		Me.MenuStrip1.Size = New System.Drawing.Size(798, 24)
		Me.MenuStrip1.TabIndex = 4
		Me.MenuStrip1.Text = "MenuStrip1"
		'
		'ToolStripMenuItem1
		'
		Me.ToolStripMenuItem1.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.HistoryToolStripMenuItem})
		Me.ToolStripMenuItem1.Name = "ToolStripMenuItem1"
		Me.ToolStripMenuItem1.Size = New System.Drawing.Size(44, 20)
		Me.ToolStripMenuItem1.Text = "&Stuff"
		'
		'HistoryToolStripMenuItem
		'
		Me.HistoryToolStripMenuItem.Name = "HistoryToolStripMenuItem"
		Me.HistoryToolStripMenuItem.Size = New System.Drawing.Size(110, 22)
		Me.HistoryToolStripMenuItem.Text = "&history"
		'
		'Form1
		'
		Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
		Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
		Me.ClientSize = New System.Drawing.Size(798, 568)
		Me.Controls.Add(Me.SplitContainer1)
		Me.Controls.Add(Me.MenuStrip1)
		Me.MainMenuStrip = Me.MenuStrip1
		Me.Name = "Form1"
		Me.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show
		Me.Text = "Form1"
		Me.SplitContainer1.Panel1.ResumeLayout(False)
		Me.SplitContainer1.Panel1.PerformLayout()
		Me.SplitContainer1.Panel2.ResumeLayout(False)
		Me.SplitContainer1.Panel2.PerformLayout()
		CType(Me.SplitContainer1, System.ComponentModel.ISupportInitialize).EndInit()
		Me.SplitContainer1.ResumeLayout(False)
		Me.MenuStrip1.ResumeLayout(False)
		Me.MenuStrip1.PerformLayout()
		Me.ResumeLayout(False)
		Me.PerformLayout()

	End Sub

	Friend WithEvents tcmd As TextBox
	Friend WithEvents tstk As TextBox
	Friend WithEvents tinf As TextBox
	Friend WithEvents SplitContainer1 As SplitContainer
	Friend WithEvents MenuStrip1 As MenuStrip
	Friend WithEvents ToolStripMenuItem1 As ToolStripMenuItem
	Friend WithEvents HistoryToolStripMenuItem As ToolStripMenuItem
End Class
