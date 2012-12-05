/*
 * Created by SharpDevelop.
 * User: chris.hall
 * Date: 31/10/2012
 * Time: 18:38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Activities.Presentation.Metadata;
using System.Activities.Presentation.Toolbox;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ICSharpCode.SharpDevelop.Dom;
using ICSharpCode.SharpDevelop.Project;

namespace SDWF4
{
	/// <summary>
	/// Description of WFToolbox.
	/// </summary>
	internal class WFToolbox
	{	
	    static ResourceDictionary iconsDict;
		
	    #region Private
	    private static string GetResourceName(string typeName)
	    {
	        string resourceKey = typeName;
	        resourceKey = resourceKey.Split('.').Last();
	        resourceKey = resourceKey.Split('`').First();
	 
	        if (resourceKey == "Flowchart")
	        {
	            // it appears that themes/icons.xaml has a typo here
	            resourceKey = "FlowChart";        }
	 
	        if (!resourceKey.EndsWith("Icon"))
	        {
	            resourceKey += "Icon";
	        }
	 
	        return resourceKey;
	    }
	        
	    /// <summary>
	    /// Loads the standard activity icons from System.Activities.Presentation.
	    /// Accepts various forms of input including 'short name' i.e. toolbox display name, and 'Type.FullName' - and tries to fix them automatically.
	    /// </summary>
	    private static object ExtractIconResource(string iconOrtypeName)
	    {
	        iconsDict = iconsDict ?? new ResourceDictionary
	        {
	            Source = new Uri("pack://application:,,,/System.Activities.Presentation;component/themes/icons.xaml")
	        };
	 
	        string resourceKey = GetResourceName(iconOrtypeName);
	        object resource = iconsDict.Contains(resourceKey) ? iconsDict[resourceKey] : null;
	 
	        if (!(resource is DrawingBrush))
	        {
	            resource = iconsDict["GenericLeafActivityIcon"];
	        }
	 
	        return resource;
	    }
	 
	    private static object ExtractIconResource(Type type)
	    {
	        string typeName = type.IsGenericType ? type.GetGenericTypeDefinition().Name : type.Name;
	        return ExtractIconResource(typeName);
	    }
	 
		private static BitmapSource BitmapSourceFromBrush(System.Windows.Media.Brush drawingBrush, int size = 32, int dpi = 96)
		{
		    var pixelFormat = PixelFormats.Pbgra32;
		    RenderTargetBitmap rtb = new RenderTargetBitmap(size, size, dpi, dpi, pixelFormat);
		    var drawingVisual = new DrawingVisual();
		    using (DrawingContext context = drawingVisual.RenderOpen())
		    {
		        context.DrawRectangle(drawingBrush, null, new Rect(0, 0, size, size));
		    }
		    rtb.Render(drawingVisual);
		    return rtb;
		}

		private static System.Drawing.Bitmap BitmapFromBitmapSource(BitmapSource source, System.Drawing.Imaging.PixelFormat pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppArgb)
		{
		    var bmp = new System.Drawing.Bitmap(source.PixelWidth, source.PixelHeight, pixelFormat);
		    BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, pixelFormat);
		    source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
		    bmp.UnlockBits(data);
		    return bmp;
		}
		
		private static void CreateToolboxBitmapAttributeForActivity(AttributeTableBuilder builder, Type builtInActivityType)
		{
			Object o = ExtractIconResource(builtInActivityType);
			var bs = BitmapSourceFromBrush((System.Windows.Media.Brush)o);
			Bitmap bitmap = BitmapFromBitmapSource(bs);
			
			if (bitmap != null)
			{
			  Type tbaType = typeof(System.Drawing.ToolboxBitmapAttribute);
			   Type imageType = typeof(System.Drawing.Image);
			   ConstructorInfo constructor = tbaType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { imageType, imageType }, null);
			   System.Drawing.ToolboxBitmapAttribute tba = constructor.Invoke(new object[] { bitmap, bitmap }) as System.Drawing.ToolboxBitmapAttribute;
			   builder.AddCustomAttributes(builtInActivityType, tba);
			}
		}

		private void InitialiseToolbox()
		{
		   Control = new ToolboxControl();
		   AttributeTableBuilder builder = new AttributeTableBuilder();
				
		   foreach (Type type in typeof(System.Activities.Activity).Assembly.GetTypes().Where(t => t.Namespace == "System.Activities.Statements"))
		   {
		       CreateToolboxBitmapAttributeForActivity(builder, type);
		   }
		   foreach (Type type in typeof(System.ServiceModel.Activities.Receive).Assembly.GetTypes().Where(t => t.Namespace == "System.ServiceModel.Activities"))
		   {
		       CreateToolboxBitmapAttributeForActivity(builder, type);
		   }
		   foreach (Type type in typeof(System.ServiceModel.Activities.Presentation.Factories.ReceiveAndSendReplyFactory).Assembly.GetTypes().Where(t => t.Namespace == "System.ServiceModel.Activities.Presentation.Factories"))
		   {
		       CreateToolboxBitmapAttributeForActivity(builder, type);
		   }
		   foreach (Type type in typeof(System.Activities.Statements.Interop).Assembly.GetTypes().Where(t => t.Namespace == "System.Activities.Statements"))
		   {
		       CreateToolboxBitmapAttributeForActivity(builder, type);
		   }
		
		   MetadataStore.AddAttributeTable(builder.CreateTable());
		   
		   var cat = new ToolboxCategory("Control Flow");
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.DoWhile)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.ForEach<>),"ForEach<T>"));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.If)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Parallel)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.ParallelForEach<>),"ParallelForEach<T>"));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Pick)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.PickBranch)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Sequence)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Switch<>),"Switch<T>"));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.While)));
		   Control.Categories.Add(cat);
		           
		   cat = new ToolboxCategory("FlowChart");
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Flowchart)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.FlowDecision)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.FlowSwitch<>),"FlowSwitch<T>"));
		   Control.Categories.Add(cat);

		   cat = new ToolboxCategory("Messaging");
		   cat.Add(new ToolboxItemWrapper(typeof(System.ServiceModel.Activities.CorrelationScope)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.ServiceModel.Activities.InitializeCorrelation)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.ServiceModel.Activities.Receive)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.ServiceModel.Activities.Presentation.Factories.ReceiveAndSendReplyFactory),"ReceiveAndSendReply"));
		   cat.Add(new ToolboxItemWrapper(typeof(System.ServiceModel.Activities.Send)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.ServiceModel.Activities.Presentation.Factories.SendAndReceiveReplyFactory),"SendAndReceiveReply"));
		   cat.Add(new ToolboxItemWrapper(typeof(System.ServiceModel.Activities.TransactedReceiveScope)));
		   Control.Categories.Add(cat);

		   cat = new ToolboxCategory("Runtime");
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Persist)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.TerminateWorkflow)));
		   Control.Categories.Add(cat);

		   cat = new ToolboxCategory("Primitives");
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Assign)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Delay)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.InvokeMethod)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.WriteLine)));
		   Control.Categories.Add(cat);
		   
		   cat = new ToolboxCategory("Transaction");
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.CancellationScope)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.CompensableActivity)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Compensate)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Confirm)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.TransactionScope)));
		   Control.Categories.Add(cat);

		   cat = new ToolboxCategory("Collection");
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.AddToCollection<>),"AddToCollection<T>"));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.ClearCollection<>),"ClearCollection<T>"));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.ExistsInCollection<>),"ExistsInCollection<T>"));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.RemoveFromCollection<>),"RemoveFromCollection<T>"));
		   Control.Categories.Add(cat);

		   cat = new ToolboxCategory("Error Handling");
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Rethrow)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Throw)));
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.TryCatch)));
		   Control.Categories.Add(cat);

		   cat = new ToolboxCategory("Migration");
		   cat.Add(new ToolboxItemWrapper(typeof(System.Activities.Statements.Interop)));
		   Control.Categories.Add(cat);
		}
		#endregion
		
		internal WFToolbox()
		{
			InitialiseToolbox();
		}
		
		static string GetHash(string fileName)
		{
			return Path.GetFileName(fileName).ToLowerInvariant() + File.GetLastWriteTimeUtc(fileName).Ticks.ToString();
		}
						
		internal ToolboxControl Control { get; private set; }
		internal void Update(List<IClass> classes)
		{
			// Get the list of proejcts that need reloading.
			var projectContents = (from c in classes select c.ProjectContent).Distinct();
			// Reload the assemblies and clear/create toolbox categories
			foreach(var pc in projectContents)
			{
				if (pc.Project != null)
				{
					var cp = (CompilableProject)pc.Project;
					var cat = Control.Categories.FirstOrDefault(ct => ct.CategoryName == cp.AssemblyName);
                    if (cat == null)
                    {
                        cat = new ToolboxCategory(cp.AssemblyName);
                        Control.Categories.Add(cat);
                    }
                    else
                    {
                        cat.Tools.Clear();
                    }
                    Assembly asm = null;
                    // Overwrite the assembly in the cache, we need to reload as we have rebuilt (memory will be used!).
                    asm = Assembly.Load(File.ReadAllBytes(cp.OutputAssemblyFullPath));
                    if (asm != null)
                    {
                    	var tbItems = from tbc in classes where tbc.ProjectContent.AssemblyName == asm.GetName().Name select tbc;
                    	foreach(var cl in tbItems)
                        {
                            var t = asm.GetType(cl.FullyQualifiedName);
                            if (!cat.Tools.Any(ti => ti.Type == t))
                                cat.Add(new ToolboxItemWrapper(t, cl.Name));
                        }
                    }
				}
			}
		}
	}
}
