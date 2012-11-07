/*
 * Created by SharpDevelop.
 * User: chris.hall
 * Date: 31/10/2012
 * Time: 11:13
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Xml;

using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Gui;

namespace SDWF4
{
	/// <summary>
	/// Secondary display binding to provide a design surface for WF xaml files.
	/// </summary>
	public class WFSecondaryDisplayBinding : ISecondaryDisplayBinding
	{	
		public bool ReattachWhenParserServiceIsReady {
			get {
				return false;
			}
		}
		
		public bool CanAttachTo(ICSharpCode.SharpDevelop.Gui.IViewContent content)
		{
			// Only attach to xaml files.
			if (Path.GetExtension(content.PrimaryFileName).Equals(".xaml", StringComparison.OrdinalIgnoreCase)) {
				IEditable editable = content as IEditable;
				if (editable != null) {
					try {
						// WF xaml files will start with an "Activity" this is used to distinguish them from
						// other xaml files such as WPF.
						XmlTextReader r = new XmlTextReader(new StringReader(editable.Text));
						r.XmlResolver = null;
						r.WhitespaceHandling = WhitespaceHandling.None;
						while (r.NodeType != XmlNodeType.Element && r.Read());
						if(r.LocalName!="Activity")
							return false;						
					} catch (XmlException) {
						return true;
					}
					return true;
				}
			}
			return false;
		}
		
		public ICSharpCode.SharpDevelop.Gui.IViewContent[] CreateSecondaryViewContent(ICSharpCode.SharpDevelop.Gui.IViewContent viewContent)
		{
			return new IViewContent[] { new WFViewContent(viewContent.PrimaryFile) };
		}
	}
}
