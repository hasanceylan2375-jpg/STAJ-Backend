using STAJ.Models;
namespace STAJ.Services;
public class KrediOdemePlanService
{
 public KrediOdemePlanResponse Hesapla(KrediOdemePlanRequest r)
 {
  if(r.KrediTutari<=0||r.Vade<=0||r.Periyot<=0) throw new ArgumentException("Kredi tutarı, vade ve periyot pozitif olmalıdır.");
  if(r.FaizOrani<0||r.BsmvOrani<0||r.KkdfOrani<0) throw new ArgumentException("Oranlar negatif olamaz.");
  if(r.Vade%r.Periyot!=0) throw new ArgumentException("Vade periyoda tam bölünmelidir.");
  var n=r.Vade/r.Periyot; var faiz=r.FaizOrani/100m*r.Periyot/12m; var kalan=r.KrediTutari; var plan=new List<KrediOdemeSatiri>(n);
  if(r.Tip.Equals("EsitAnaparali",StringComparison.OrdinalIgnoreCase)) {
   var ana=Math.Round(r.KrediTutari/n,2,MidpointRounding.AwayFromZero);
   for(var i=1;i<=n;i++){var a=i==n?kalan:Math.Min(ana,kalan);var f=Math.Round(kalan*faiz,2);var b=Math.Round(f*r.BsmvOrani/100m,2);var k=Math.Round(f*r.KkdfOrani/100m,2);kalan=Math.Max(0,Math.Round(kalan-a,2));plan.Add(new(){TaksitNo=i,Anapara=a,Faiz=f,Bsmv=b,Kkdf=k,Taksit=a+f+b+k,KalanAnapara=kalan});}
  } else {
   var oran=faiz*(1m+(r.BsmvOrani+r.KkdfOrani)/100m); var taksit=oran==0?r.KrediTutari/n:r.KrediTutari*oran/(1m-(decimal)Math.Pow((double)(1m+oran),-n)); taksit=Math.Round(taksit,2);
   for(var i=1;i<=n;i++){var f=Math.Round(kalan*faiz,2);var b=Math.Round(f*r.BsmvOrani/100m,2);var k=Math.Round(f*r.KkdfOrani/100m,2);var a=i==n?kalan:Math.Min(kalan,Math.Round(taksit-f-b-k,2));kalan=Math.Max(0,Math.Round(kalan-a,2));plan.Add(new(){TaksitNo=i,Anapara=a,Faiz=f,Bsmv=b,Kkdf=k,Taksit=a+f+b+k,KalanAnapara=kalan});}
  }
  return new(){Tip=r.Tip.Equals("EsitAnaparali",StringComparison.OrdinalIgnoreCase)?"EsitAnaparali":"EsitTaksitli",KrediTutari=r.KrediTutari,Vade=r.Vade,FaizOrani=r.FaizOrani,BsmvOrani=r.BsmvOrani,KkdfOrani=r.KkdfOrani,Periyot=r.Periyot,ToplamAnapara=plan.Sum(x=>x.Anapara),ToplamFaiz=plan.Sum(x=>x.Faiz),ToplamBsmv=plan.Sum(x=>x.Bsmv),ToplamKkdf=plan.Sum(x=>x.Kkdf),ToplamOdeme=plan.Sum(x=>x.Taksit),Plan=plan};
 }
}
