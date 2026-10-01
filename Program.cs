using System.Globalization;
using System.Numerics;
using System.Text;

record Node(string Key,string Label,List<string> V,List<Node> C);
class Tok {
  string s; int p;
  public Tok(string x){s=x;}
  public IEnumerable<string> Get(){
    while(p<s.Length){
      if(char.IsWhiteSpace(s[p])){p++;continue;}
      if(s[p]=='/'&&p+1<s.Length&&s[p+1]=='/'){while(p<s.Length&&s[p]!='\n')p++;continue;}
      if(s[p]=='('||s[p]==')'){yield return s[p++].ToString();continue;}
      if(s[p]=='"'){p++;var b=new StringBuilder();while(p<s.Length&&s[p]!='"'){if(s[p]=='\\'&&p+1<s.Length)p++;b.Append(s[p++]);}if(p<s.Length)p++;yield return "\""+b+"\"";continue;}
      int a=p;while(p<s.Length&&!char.IsWhiteSpace(s[p])&&s[p]!='('&&s[p]!=')')p++;yield return s[a..p];
    }
  }
}
class Parser {
  List<string> t;int i;
  public Parser(string s){t=new Tok(s).Get().ToList();}
  static bool Num(string x)=>double.TryParse(x.Trim('"'),NumberStyles.Float,CultureInfo.InvariantCulture,out _);
  Node? B(){
    if(i>=t.Count)return null; string k=t[i++],l="";
    if(i<t.Count&&t[i]!="("&&t[i]!=")"&&!Num(t[i]))l=t[i++].Trim('"');
    if(i>=t.Count||t[i++]!="(")return new Node(k,l,[],[]);
    var v=new List<string>();var c=new List<Node>();
    while(i<t.Count&&t[i]!=")"){
      int q=i; if(i+1<t.Count&&t[i+1]=="("){var n=B();if(n!=null){c.Add(n);continue;}i=q;}
      v.Add(t[i++].Trim('"'));
    }
    if(i<t.Count)i++; return new Node(k,l,v,c);
  }
  public Node Parse()=>B()??throw new Exception("Invalid shape");
}
static class U {
 public static IEnumerable<Node> A(Node n,string k){if(n.Key.Equals(k,StringComparison.OrdinalIgnoreCase))yield return n;foreach(var c in n.C)foreach(var x in A(c,k))yield return x;}
 public static Node? F(Node n,string k)=>A(n,k).FirstOrDefault();
 public static int I(string s)=>int.Parse(s,CultureInfo.InvariantCulture);
 public static float F(string s)=>float.Parse(s,CultureInfo.InvariantCulture);
}
class App {
 public static void Run(string input,string output){
  string s;using(var r=new StreamReader(input,Encoding.Unicode,true))s=r.ReadToEnd();
  int z=s.IndexOf("SIMISA",StringComparison.OrdinalIgnoreCase);if(z<0)throw new Exception("Not an uncompressed MSTS .S file.");
  var root=new Parser(s[z..]).Parse();
  var pn=U.F(root,"points")??throw new Exception("No points table.");
  var pts=pn.C.Where(x=>x.Key.Equals("point",StringComparison.OrdinalIgnoreCase)).Select(x=>new Vector3(U.F(x.V[0]),U.F(x.V[1]),U.F(x.V[2]))).ToList();
  var mn=U.F(root,"matrices");
  var mats=mn?.C.Where(x=>x.Key.Equals("matrix",StringComparison.OrdinalIgnoreCase)).Select(x=>{var a=x.V.Select(U.F).ToArray();return a.Length>=12?new Matrix4x4(a[0],a[1],a[2],0,a[3],a[4],a[5],0,a[6],a[7],a[8],0,a[9],a[10],a[11],1):Matrix4x4.Identity;}).ToList()??[];
  var hn=U.F(root,"hierarchy");var par=hn?.V.Select(U.I).ToList()??Enumerable.Repeat(-1,mats.Count).ToList();
  Matrix4x4 W(int i){var m=Matrix4x4.Identity;var seen=new HashSet<int>();while(i>=0&&i<mats.Count&&seen.Add(i)){m=m*mats[i];i=i<par.Count?par[i]:-1;}return m;}
  var vn=U.F(root,"vtx_states");var states=vn?.C.Where(x=>x.Key.Equals("vtx_state",StringComparison.OrdinalIgnoreCase)).Select(x=>x.V.Count>1?U.I(x.V[1]):0).ToList()??[0];
  var outv=new List<Vector3>();var faces=new List<(int,int,int)>();
  foreach(var sub in U.A(root,"sub_object")){
    var verts=U.F(sub,"vertices")?.C.Where(x=>x.Key.Equals("vertex",StringComparison.OrdinalIgnoreCase)).ToList();if(verts==null)continue;
    var matrix=Enumerable.Repeat(0,verts.Count).ToArray();
    var sets=U.F(sub,"vertex_sets");
    if(sets!=null)foreach(var q in sets.C.Where(x=>x.Key.Equals("vertex_set",StringComparison.OrdinalIgnoreCase))){
      if(q.V.Count<3)continue;int si=U.I(q.V[0]),st=U.I(q.V[1]),ct=U.I(q.V[2]);int mi=si>=0&&si<states.Count?states[si]:0;
      for(int j=st;j<Math.Min(st+ct,matrix.Length);j++)matrix[j]=mi;
    }
    int b=outv.Count;
    for(int j=0;j<verts.Count;j++){int pi=verts[j].V.Count>1?U.I(verts[j].V[1]):0;var p=pi>=0&&pi<pts.Count?pts[pi]:Vector3.Zero;int mi=matrix[j];if(mi>=0&&mi<mats.Count)p=Vector3.Transform(p,W(mi));outv.Add(p);}
    var pr=U.F(sub,"primitives");if(pr==null)continue;
    foreach(var cmd in pr.C.Where(x=>x.Key.Equals("indexed_trilist",StringComparison.OrdinalIgnoreCase))){
      var ix=U.F(cmd,"vertex_idxs");if(ix==null)continue;for(int j=0;j+2<ix.V.Count;j+=3){int a=b+U.I(ix.V[j]),c=b+U.I(ix.V[j+1]),d=b+U.I(ix.V[j+2]);if(a>=b&&d<outv.Count)faces.Add((a,c,d));}
    }
  }
  if(outv.Count==0||faces.Count==0)throw new Exception("No triangle geometry found. This S variant needs additional parser support.");
  Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
  var mtl=Path.ChangeExtension(output,".mtl");
  using(var w=new StreamWriter(output,false,new UTF8Encoding(false))){w.WriteLine("mtllib "+Path.GetFileName(mtl));foreach(var p in outv)w.WriteLine($"v {p.X.ToString("G9",CultureInfo.InvariantCulture)} {p.Y.ToString("G9",CultureInfo.InvariantCulture)} {(-p.Z).ToString("G9",CultureInfo.InvariantCulture)}");foreach(var f in faces)w.WriteLine($"f {f.Item1+1} {f.Item2+1} {f.Item3+1}");}
  File.WriteAllText(mtl,"newmtl MSTS_Default\nKd 1 1 1\nKa 0 0 0\nKs 0 0 0\n");
  Console.WriteLine($"OK: {outv.Count} vertices, {faces.Count} triangles");
 }
}
class Program{static void Main(string[] a){try{if(a.Length<1){Console.WriteLine("ShapeToObj Own\nUsage: ShapeToObj.exe input.s [output.obj]");return;}App.Run(a[0],a.Length>1?a[1]:Path.ChangeExtension(a[0],".obj"));}catch(Exception e){Console.Error.WriteLine("ERROR: "+e.Message);Environment.ExitCode=1;}}}