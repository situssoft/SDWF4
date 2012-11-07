/*
 * Created by SharpDevelop.
 * User: chris.hall
 * Date: 31/10/2012
 * Time: 11:51
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Activities.Core.Presentation;
using System.Activities.Presentation;
using System.Activities.Presentation.Metadata;
using System.Activities.Presentation.Toolbox;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;

namespace SDWF4
{
	/// <summary>
	/// Description of WFViewContent.
	/// </summary>
	public class WFViewContent : AbstractViewContentHandlingLoadErrors, IHasPropertyContainer, IToolsHost
	{
		WFToolbox toolbox;
		public WFViewContent(OpenedFile file) : base(file)
		{
			this.TabPageText = "WF-Design";
			
			// Subscribe to build events so that we can update the toolbox with activities that
			// are built as part of the solution.
			ProjectService.BuildFinished += ProjectService_BuildFinished;
			toolbox = new WFToolbox();
			var classes = ScanProjectAssemblies();
			toolbox.Update(classes);
		}

		void ProjectService_BuildFinished(object sender, BuildEventArgs e)
		{
			var classes = ScanProjectAssemblies();
			toolbox.Update(classes);
		}

		public override void Dispose()
		{
			if (!IsDisposed)
			{
				ProjectService.BuildFinished -= ProjectService_BuildFinished;
				base.Dispose();
			}
		}
		
		/// <summary>
		/// Gets the list of project contents of all open projects plus the referenced project contents.
		/// </summary>
		static IEnumerable<IProjectContent> AllProjectContentsWithReferences {
			get {
				return Enumerable.Union(ParserService.AllProjectContents,
				                        AssemblyParserService.DefaultProjectContentRegistry.GetLoadedProjectContents());
			}
		}

		/// <summary>
		/// Get a list of classes in the project that are derived from "System.Activities.Activity"
		/// </summary>
		List<IClass> ScanProjectAssemblies()
		{
			List<IClass> classes = new List<IClass>();
			
			foreach (IProjectContent pc in AllProjectContentsWithReferences) {
				if (pc.Project == null) {
					ReflectionProjectContent rpc = pc as ReflectionProjectContent;
					if (rpc == null)
						continue;
					if (rpc.IsGacAssembly)
						continue;
				}
				foreach (IClass c in pc.Classes) {
					var ctors = c.Methods.Where(method => method.IsConstructor);
					if (ctors.Any() && !ctors.Any(
						(IMethod method) => method.IsPublic && method.Parameters.Count == 0
					)) {
						// do not include classes that don't have a public parameterless constructor
						continue;
					}
					foreach (IClass subClass in c.ClassInheritanceTree) {
						if (subClass.FullyQualifiedName == "System.Activities.Activity") {
							classes.Add(c);
						}
					}
				}
			}
			return classes;
		}
				
		private MemoryStream _stream;
		private bool wasChangedInDesigner;
		
		protected override void SaveInternal(ICSharpCode.SharpDevelop.OpenedFile file, System.IO.Stream stream)
		{
			if (wasChangedInDesigner) {
				using (var writer = new StreamWriter(stream)) {
					wfDesigner.Flush();
					writer.Write(wfDesigner.Text);
				}
			} 
			else 
			{
				_stream.Position = 0;
				using (var reader = new StreamReader(new UnclosableStream(_stream))) {
					using (var writer = new StreamWriter(stream)) {
						writer.Write(reader.ReadToEnd());
					}
				}
			}
		}
		
		WorkflowDesigner wfDesigner;
		protected override void LoadInternal(ICSharpCode.SharpDevelop.OpenedFile file, System.IO.Stream stream)
		{
			wasChangedInDesigner = false;
			Debug.Assert(file == this.PrimaryFile);
			
			_stream = new MemoryStream();
			stream.CopyTo(_stream);
			stream.Position = 0;

			// register metadata - required when re-hosting the WF designer.
   			(new DesignerMetadata()).Register();

			// create the workflow designer
			wfDesigner = new WorkflowDesigner();
			wfDesigner.ModelChanged += delegate { PrimaryFile.IsDirty = wasChangedInDesigner = true; };

			// Setup the designer in SD.
			this.UserContent = wfDesigner.View;
			propertyContainer.PropertyGridReplacementContent = wfDesigner.PropertyInspectorView;

			try
			{
				wfDesigner.Text = (new StreamReader(stream).ReadToEnd());
				wfDesigner.Load();
			}
			catch(Exception e)
			{
				this.UserContent = e;
			}
		}
		
		PropertyContainer propertyContainer = new PropertyContainer();
		public PropertyContainer PropertyContainer {
			get {
				return propertyContainer;
			}
		}
		
		public object ToolsContent {
			get {
				Debug.Assert(toolbox != null);
				return toolbox.Control;
			}
		}
	}
}
