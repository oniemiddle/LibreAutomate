#if DEBUG || IDE_LA
extern alias CAW;
//using System.Windows.Forms;

namespace LA;

static class Test {
	/// <summary>
	/// 
	/// </summary>
	public static void FromMenubar() {
		//print.clear();
		
		print.it(Panels.Editor.ActiveDoc.aaaCurrentPos16);
		
		//var query = Panels.Editor.ActiveDoc.aaaText.Lines()[0];
		//var query = Panels.Editor.ActiveDoc.aaaText;
		//Task.Run(() => {
		//	try {
		//		McpTools _tools = new();
		//		var s = _tools.find_la_docs(query, "");
		//		print.scrollToTop();
		//	}
		//	catch (Exception ex) { print.it(ex); }
		//});
		//print.it(s);
		
		//timer2.every(500, _=> { GC.Collect(); });
		
		//Cpp.Cpp_Test();
		
#if !IDE_LA
#endif
	}
	
	public static void MonitorGC() {
		//if(!s_debug2) {
		//	s_debug2 = true;
		//	new TestGC();
		
		//	//timer.every(50, _ => {
		//	//	if(!s_debug) {
		//	//		s_debug = true;
		//	//		timer.after(100, _ => new TestGC());
		//	//	}
		//	//});
		//}
	}
	//static bool s_debug2;
	
	class TestGC {
		~TestGC() {
			if (Environment.HasShutdownStarted) return;
			if (AppDomain.CurrentDomain.IsFinalizingForUnload()) return;
			print.it("GC", GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
			//timer.after(1, _ => new TestGC());
			//var f = App.Wmain; if(!f.IsHandleCreated) return;
			//f.BeginInvoke(new Action(() => new TestGC()));
			new TestGC();
		}
	}
}
#endif
