// Copyright (c) DotSpatial Team. All rights reserved.
// Licensed under the MIT license. See License.txt file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using DotSpatial.Data;
using DotSpatial.Serialization;

namespace DotSpatial.Symbology.Forms
{
    /// <summary>
    /// DetailedPolygonSymbolControl.
    /// </summary>
    public partial class DetailedPolygonSymbolControl : UserControl
    {
        #region Fields

        private bool _disableUnitWarning;
        private bool _ignoreChanges;
        private IPolygonSymbolizer _original;
        private IPolygonSymbolizer _symbolizer;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailedPolygonSymbolControl"/> class.
        /// </summary>
        public DetailedPolygonSymbolControl()
        {
            InitializeComponent();
            Configure();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailedPolygonSymbolControl"/> class
        /// where the properties displayed on the form are a duplicate of the original properties,
        /// and those properties will be copied back to the original on an apply changes or an ok click.
        /// </summary>
        /// <param name="original">The original polygon symbolizer.</param>
        public DetailedPolygonSymbolControl(IPolygonSymbolizer original)
            : this()
        {
            Initialize(original);
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the the Add To Custom Symbols button is clicked
        /// </summary>
        public event EventHandler<PolygonSymbolizerEventArgs> AddToCustomSymbols;

        /// <summary>
        /// Occurs whenever the apply changes button is clicked, or else when the ok button is clicked.
        /// </summary>
        public event EventHandler ChangesApplied;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current (copied) symbolizer or initializes this control to work with the
        /// specified symbolizer as the original.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IPolygonSymbolizer Symbolizer
        {
            get
            {
                return _symbolizer;
            }

            set
            {
                if (value != null) Initialize(value);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Forces the current settings to be written back to the original symbolizer.
        /// </summary>
        public void ApplyChanges()
        {
            OnApplyChanges();
        }

        /// <summary>
        /// Assigns the original symbolizer to this control.
        /// </summary>
        /// <param name="original">The polygon symbolizer to modify.</param>
        public void Initialize(IPolygonSymbolizer original)
        {
            _symbolizer = original.Copy();
            _original = original;
            ccPatterns.Patterns = _symbolizer.Patterns;
            ccPatterns.RefreshList();
            if (_symbolizer.Patterns.Count > 0)
            {
                ccPatterns.SelectedPattern = _symbolizer.Patterns[0];
            }

            ocOutline.Pattern = ccPatterns.SelectedPattern;
            UpdatePreview();
            UpdatePatternControls();
        }

        /// <summary>
        /// Fires the AddToCustomSymbols event.
        /// </summary>
        protected virtual void OnAddToCustomSymbols()
        {
            AddToCustomSymbols?.Invoke(this, new PolygonSymbolizerEventArgs(_symbolizer));
        }

        /// <summary>
        /// Fires the ChangesApplied event.
        /// </summary>
        protected virtual void OnApplyChanges()
        {
            _original.CopyProperties(_symbolizer);
            ChangesApplied?.Invoke(this, EventArgs.Empty);
        }

        private void AngGradientAngleAngleChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IGradientPattern gp = ccPatterns.SelectedPattern as IGradientPattern;
            if (gp != null)
            {
                gp.Angle = angGradientAngle.Angle;
            }

            UpdatePreview();
        }

        private void AngTileAngleAngleChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IPicturePattern pp = ccPatterns.SelectedPattern as IPicturePattern;
            if (pp != null)
            {
                pp.Angle = angTileAngle.Angle;
            }

            UpdatePreview();
        }

        private void BtnAddToCustomClick(object sender, EventArgs e)
        {
            OnAddToCustomSymbols();
        }

        private void BtnLoadImageClick(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IPicturePattern pp = ccPatterns.SelectedPattern as IPicturePattern;
            if (pp != null)
            {
                using (OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = pp.DialogFilter
                })
                {
                    if (ofd.ShowDialog() != DialogResult.OK) return;

                    pp.Open(ofd.FileName);
                    txtImage.Text = Path.GetFileName(ofd.FileName);
                }
            }

            UpdatePreview();
        }

        /// <summary>
        /// Updates the pattern when the simple fill color changes.
        /// </summary>
        /// <param name="sender">Sender that raised the event.</param>
        /// <param name="e">The event args.</param>
        private void CbColorSimpleColorChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            ISimplePattern sp = ccPatterns.SelectedPattern as ISimplePattern;
            if (sp != null) sp.FillColor = cbColorSimple.Color;
            sldOpacitySimple.Value = cbColorSimple.Color.GetOpacity();
            sldOpacitySimple.MaximumColor = cbColorSimple.Color.ToOpaque();
            UpdatePreview();
        }

        private void CcPatternsAddClicked(object sender, EventArgs e)
        {
            string patternType = (string)cmbPatternType.SelectedItem;
            switch (patternType)
            {
                case "Simple":
                    ccPatterns.Patterns.Insert(0, new SimplePattern());
                    break;
                case "Picture":
                    ccPatterns.Patterns.Insert(0, new PicturePattern());
                    break;
                case "Gradient":
                    ccPatterns.Patterns.Insert(0, new GradientPattern());
                    break;
            }

            ccPatterns.RefreshList();
            ccPatterns.SelectedPattern = ccPatterns.Patterns[0];
            UpdatePreview();
        }

        private void CcPatternsSelectedItemChanged(object sender, EventArgs e)
        {
            if (ccPatterns.SelectedPattern != null)
            {
                UpdatePatternControls();
            }

            UpdatePreview();
        }

        private void ChkSmoothingCheckedChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            _symbolizer.Smoothing = chkSmoothing.Checked;
            UpdatePreview();
        }

        private void CmbGradientTypeSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IGradientPattern gp = ccPatterns.SelectedPattern as IGradientPattern;
            if (gp != null)
            {
                gp.GradientType = Global.ParseEnum<GradientType>((string)cmbGradientType.SelectedItem);
                if (gp.GradientType == GradientType.Linear)
                {
                    lblStartColor.Text = SymbologyFormsMessageStrings.DetailedPolygonSymbolControl_StartColor;
                    lblEndColor.Text = SymbologyFormsMessageStrings.DetailedPolygonSymbolControl_EndColor;
                }
                else
                {
                    lblStartColor.Text = SymbologyFormsMessageStrings.DetailedPolygonSymbolControl_SurroundColor;
                    lblEndColor.Text = SymbologyFormsMessageStrings.DetailedPolygonSymbolControl_CenterColor;
                }
            }

            UpdatePreview();
        }

        private void CmbHatchStyleSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IHatchPattern hp = ccPatterns.SelectedPattern as IHatchPattern;
            if (hp != null) hp.HatchStyle = (HatchStyle)cmbHatchStyle.SelectedItem;
            UpdatePreview();
        }

        private void CmbPatternTypeSelectedIndexChanged(object sender, EventArgs e)
        {
            if ((string)cmbPatternType.SelectedItem == "Simple")
            {
                if (tabPatternProperties.TabPages.Contains(tabPicture))
                {
                    tabPatternProperties.TabPages.Remove(tabPicture);
                }

                if (tabPatternProperties.TabPages.Contains(tabGradient))
                {
                    tabPatternProperties.TabPages.Remove(tabGradient);
                }

                if (tabPatternProperties.TabPages.Contains(tabHatch))
                {
                    tabPatternProperties.TabPages.Remove(tabHatch);
                }

                if (tabPatternProperties.TabPages.Contains(tabSimple) == false)
                {
                    tabPatternProperties.TabPages.Add(tabSimple);
                    tabPatternProperties.SelectedTab = tabSimple;
                }
            }

            if ((string)cmbPatternType.SelectedItem == "Picture")
            {
                if (tabPatternProperties.TabPages.Contains(tabSimple))
                {
                    tabPatternProperties.TabPages.Remove(tabSimple);
                }

                if (tabPatternProperties.TabPages.Contains(tabGradient))
                {
                    tabPatternProperties.TabPages.Remove(tabGradient);
                }

                if (tabPatternProperties.TabPages.Contains(tabHatch))
                {
                    tabPatternProperties.TabPages.Remove(tabHatch);
                }

                if (tabPatternProperties.TabPages.Contains(tabPicture) == false)
                {
                    tabPatternProperties.TabPages.Add(tabPicture);
                    tabPatternProperties.SelectedTab = tabPicture;
                }
            }

            if ((string)cmbPatternType.SelectedItem == "Gradient")
            {
                if (tabPatternProperties.TabPages.Contains(tabSimple))
                {
                    tabPatternProperties.TabPages.Remove(tabSimple);
                }

                if (tabPatternProperties.TabPages.Contains(tabPicture))
                {
                    tabPatternProperties.TabPages.Remove(tabPicture);
                }

                if (tabPatternProperties.TabPages.Contains(tabHatch))
                {
                    tabPatternProperties.TabPages.Remove(tabHatch);
                }

                if (tabPatternProperties.TabPages.Contains(tabGradient) == false)
                {
                    tabPatternProperties.TabPages.Add(tabGradient);
                    tabPatternProperties.SelectedTab = tabGradient;
                }
            }

            if ((string)cmbPatternType.SelectedItem == "Hatch")
            {
                if (tabPatternProperties.TabPages.Contains(tabSimple))
                {
                    tabPatternProperties.TabPages.Remove(tabSimple);
                }

                if (tabPatternProperties.TabPages.Contains(tabPicture))
                {
                    tabPatternProperties.TabPages.Remove(tabPicture);
                }

                if (tabPatternProperties.TabPages.Contains(tabGradient))
                {
                    tabPatternProperties.TabPages.Remove(tabGradient);
                }

                if (tabPatternProperties.TabPages.Contains(tabHatch) == false)
                {
                    tabPatternProperties.TabPages.Add(tabHatch);
                    tabPatternProperties.SelectedTab = tabHatch;
                }
            }

            if (_ignoreChanges) return;

            int index = ccPatterns.Patterns.IndexOf(ccPatterns.SelectedPattern);
            if (index == -1) return;

            IPattern oldPattern = ccPatterns.SelectedPattern;
            if ((string)cmbPatternType.SelectedItem == "Simple")
            {
                SimplePattern sp = new SimplePattern();
                if (oldPattern != null) sp.CopyOutline(oldPattern);
                ccPatterns.Patterns[index] = sp;
                ccPatterns.RefreshList();
                ccPatterns.SelectedPattern = sp;
                UpdateSimplePatternControls(sp);
            }

            if ((string)cmbPatternType.SelectedItem == "Picture")
            {
                PicturePattern pp = new PicturePattern();
                if (oldPattern != null) pp.CopyOutline(oldPattern);
                ccPatterns.Patterns[index] = pp;
                ccPatterns.RefreshList();
                ccPatterns.SelectedPattern = pp;
                UpdatePicturePatternControls(pp);
            }

            if ((string)cmbPatternType.SelectedItem == "Gradient")
            {
                GradientPattern gp = new GradientPattern();
                if (oldPattern != null) gp.CopyOutline(oldPattern);
                ccPatterns.Patterns[index] = gp;
                ccPatterns.RefreshList();
                ccPatterns.SelectedPattern = gp;
                UpdateGradientPatternControls(gp);
            }

            if ((string)cmbPatternType.SelectedItem == "Hatch")
            {
                HatchPattern hp = new HatchPattern();
                if (oldPattern != null) hp.CopyOutline(oldPattern);
                ccPatterns.Patterns[index] = hp;
                ccPatterns.RefreshList();
                ccPatterns.SelectedPattern = hp;
            }
        }

        private void CmbScaleModeSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            _symbolizer.ScaleMode = Global.ParseEnum<ScaleMode>(cmbScaleMode.SelectedItem.ToString());
        }

        private void CmbTileModeSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IPicturePattern pp = ccPatterns.SelectedPattern as IPicturePattern;
            if (pp != null)
            {
                pp.WrapMode = Global.ParseEnum<WrapMode>((string)cmbTileMode.SelectedItem);
            }

            UpdatePreview();
        }

        private void CmbUnitsSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges || _disableUnitWarning) return;

            if (cmbUnits.SelectedItem.ToString() == "World" && _symbolizer.ScaleMode != ScaleMode.Geographic)
            {
                if (MessageBox.Show(SymbologyFormsMessageStrings.DetailedPolygonSymbolControl_SwitchToGeographicScaleMode, SymbologyFormsMessageStrings.DetailedPolygonSymbolControl_ChangeScaleMode, MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    cmbUnits.SelectedItem = _symbolizer.Units.ToString();
                    return;
                }

                _symbolizer.ScaleMode = ScaleMode.Geographic;
            }

            if (cmbUnits.SelectedItem.ToString() != "World" && _symbolizer.ScaleMode == ScaleMode.Geographic)
            {
                if (MessageBox.Show(SymbologyFormsMessageStrings.DetailedPolygonSymbolControl_SwitchToSymbolicScaleMode, SymbologyFormsMessageStrings.DetailedPolygonSymbolControl_ChangeScaleMode, MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    cmbUnits.SelectedItem = _symbolizer.Units.ToString();
                    return;
                }

                _symbolizer.ScaleMode = ScaleMode.Symbolic;
            }

            GraphicsUnit destination = (GraphicsUnit)Enum.Parse(typeof(GraphicsUnit), cmbUnits.SelectedItem.ToString());

            GraphicsUnit source = _symbolizer.Units;
            if (source == GraphicsUnit.Inch && destination == GraphicsUnit.Millimeter)
            {
            }

            if (source == GraphicsUnit.Millimeter && destination == GraphicsUnit.Inch)
            {
            }

            UpdatePatternControls();
        }

        private void Configure()
        {
            Array hatchs = Enum.GetValues(typeof(HatchStyle));
            foreach (object style in hatchs) cmbHatchStyle.Items.Add(style);

            ccPatterns.SelectedItemChanged += CcPatternsSelectedItemChanged;
            ccPatterns.AddClicked += CcPatternsAddClicked;
            lblPreview.Paint += LblPreviewPaint;
            ocOutline.ChangesApplied += OcOutlineChangesApplied;
        }

        private void DbxScaleXTextChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IPicturePattern pp = ccPatterns.SelectedPattern as IPicturePattern;
            if (pp != null)
            {
                pp.Scale.X = dbxScaleX.Value;
            }

            UpdatePreview();
        }

        private void DbxScaleYTextChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IPicturePattern pp = ccPatterns.SelectedPattern as IPicturePattern;
            if (pp != null)
            {
                pp.Scale.Y = dbxScaleY.Value;
            }

            UpdatePreview();
        }

        /// <summary>
        /// Updates the pattern when the hatch back color changes.
        /// </summary>
        /// <param name="sender">Sender that raised the event.</param>
        /// <param name="e">The event args.</param>
        private void HatchBackColorColorChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IHatchPattern hp = ccPatterns.SelectedPattern as IHatchPattern;
            if (hp != null) hp.BackColor = hatchBackColor.Color;
            hatchBackOpacity.Value = hatchBackColor.Color.GetOpacity();
            hatchBackOpacity.MaximumColor = hatchBackColor.Color.ToOpaque();
            UpdatePreview();
        }

        /// <summary>
        /// Updates the pattern when the hatch back color opacity changes.
        /// </summary>
        /// <param name="sender">Sender that raised the event.</param>
        /// <param name="e">The event args.</param>
        private void HatchBackOpacityValueChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IHatchPattern hp = ccPatterns.SelectedPattern as IHatchPattern;
            if (hp != null) hp.BackColorOpacity = (float)hatchBackOpacity.Value;
            hatchBackColor.Color = hatchBackOpacity.MaximumColor.ToTransparent((float)hatchBackOpacity.Value);
            UpdatePreview();
        }

        /// <summary>
        /// Updates the pattern when the hatch fore color changes.
        /// </summary>
        /// <param name="sender">Sender that raised the event.</param>
        /// <param name="e">The event args.</param>
        private void HatchForeColorColorChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IHatchPattern hp = ccPatterns.SelectedPattern as IHatchPattern;
            if (hp != null) hp.ForeColor = hatchForeColor.Color;
            hatchForeOpacity.Value = hatchForeColor.Color.GetOpacity();
            hatchForeOpacity.MaximumColor = hatchForeColor.Color.ToOpaque();
            UpdatePreview();
        }

        /// <summary>
        /// Updates the pattern when the hatch fore color opacity changes.
        /// </summary>
        /// <param name="sender">Sender that raised the event.</param>
        /// <param name="e">The event args.</param>
        private void HatchForeOpacityValueChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IHatchPattern hp = ccPatterns.SelectedPattern as IHatchPattern;
            if (hp != null) hp.ForeColorOpacity = (float)hatchForeOpacity.Value;
            hatchForeColor.Color = hatchForeOpacity.MaximumColor.ToTransparent((float)hatchForeOpacity.Value);
            UpdatePreview();
        }

        private void LblPreviewPaint(object sender, PaintEventArgs e)
        {
            UpdatePreview(e.Graphics);
        }

        private void OcOutlineChangesApplied(object sender, EventArgs e)
        {
            _original.CopyProperties(_symbolizer);
        }

        private void OcOutlineOutlineChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        /// <summary>
        /// Updates the pattern when the simple color opacity changes.
        /// </summary>
        /// <param name="sender">Sender that raised the event.</param>
        /// <param name="e">The event args.</param>
        private void SldOpacitySimpleValueChanged(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            ISimplePattern sp = ccPatterns.SelectedPattern as ISimplePattern;
            if (sp != null) sp.Opacity = (float)sldOpacitySimple.Value;
            cbColorSimple.Color = sldOpacitySimple.MaximumColor.ToTransparent((float)sldOpacitySimple.Value);
            UpdatePreview();
        }

        private void SliderGradientGradientChanging(object sender, EventArgs e)
        {
            if (_ignoreChanges) return;

            IGradientPattern gp = ccPatterns.SelectedPattern as IGradientPattern;
            if (gp != null)
            {
                gp.Colors = new[] { sliderGradient.MinimumColor, sliderGradient.MinimumColor, sliderGradient.MaximumColor, sliderGradient.MaximumColor };
                gp.Positions = new[] { 0F, sliderGradient.StartValue, sliderGradient.EndValue, 1F };
            }

            UpdatePreview();
        }

        private void UpdateGradientPatternControls(IGradientPattern pattern)
        {
            cmbPatternType.SelectedItem = "Gradient";
            cmbGradientType.SelectedItem = pattern.GradientType.ToString();
            sliderGradient.MinimumColor = pattern.Colors[0];
            sliderGradient.MaximumColor = pattern.Colors[pattern.Colors.Length - 1];
            angGradientAngle.Angle = (int)pattern.Angle;
        }

        private void UpdateHatchPatternControls(IHatchPattern pattern)
        {
            cmbPatternType.SelectedItem = "Hatch";
            cmbHatchStyle.SelectedItem = pattern.HatchStyle;
            hatchForeColor.Color = pattern.ForeColor;
            hatchForeOpacity.Value = pattern.ForeColorOpacity;
            hatchForeOpacity.MaximumColor = pattern.ForeColor.ToOpaque();
            hatchBackColor.Color = pattern.BackColor;
            hatchBackOpacity.Value = pattern.BackColorOpacity;
            hatchBackOpacity.MaximumColor = pattern.BackColor.ToOpaque();
        }

        private void UpdatePatternControls()
        {
            _ignoreChanges = true;
            cmbScaleMode.SelectedItem = _symbolizer.ScaleMode.ToString();
            chkSmoothing.Checked = _symbolizer.Smoothing;
            _disableUnitWarning = true;
            cmbUnits.SelectedItem = _symbolizer.Units.ToString();
            _disableUnitWarning = false;
            ocOutline.Pattern = ccPatterns.SelectedPattern;
            ISimplePattern sp = ccPatterns.SelectedPattern as ISimplePattern;
            if (sp != null)
            {
                UpdateSimplePatternControls(sp);
            }

            IPicturePattern pp = ccPatterns.SelectedPattern as IPicturePattern;
            if (pp != null)
            {
                UpdatePicturePatternControls(pp);
            }

            IGradientPattern gp = ccPatterns.SelectedPattern as IGradientPattern;
            if (gp != null)
            {
                UpdateGradientPatternControls(gp);
            }

            IHatchPattern hp = ccPatterns.SelectedPattern as IHatchPattern;
            if (hp != null)
            {
                UpdateHatchPatternControls(hp);
            }

            _ignoreChanges = false;
            UpdatePreview();
        }

        private void UpdatePicturePatternControls(IPicturePattern pattern)
        {
            cmbPatternType.SelectedItem = "Picture";
            txtImage.Text = Path.GetFileName(pattern.PictureFilename);
            cmbTileMode.SelectedItem = pattern.WrapMode.ToString();
            angTileAngle.Angle = (int)pattern.Angle;
            dbxScaleX.Value = pattern.Scale.X;
            dbxScaleY.Value = pattern.Scale.Y;
        }

        private void UpdatePreview(Graphics g)
        {
            g.Clear(Color.White);

            _symbolizer.Draw(g, new Rectangle(5, 5, lblPreview.Width - 10, lblPreview.Height - 10));
        }

        private void UpdatePreview()
        {
            ccPatterns.Refresh();
            Graphics g = lblPreview.CreateGraphics();
            UpdatePreview(g);
            g.Dispose();
        }

        private void UpdateSimplePatternControls(ISimplePattern pattern)
        {
            cmbPatternType.SelectedItem = "Simple";
            cbColorSimple.Color = pattern.FillColor;
            sldOpacitySimple.Value = pattern.Opacity;
            sldOpacitySimple.MaximumColor = pattern.FillColor.ToOpaque();
        }

        #endregion
    }
}