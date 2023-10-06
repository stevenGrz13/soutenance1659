using System.Net.Mime;
using System.Reflection.Metadata;
using AnaeLogiciel.Data;
using AnaeLogiciel.Models;
using GrapeCity.Documents.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnaeLogiciel.Controllers;

public class ExportPDFController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public ExportPDFController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult VersExportProjet()
    {
        return View("~/Views/ExportPDF/Choice.cshtml");
    }
    

    public async Task<IActionResult> GeneratePdf(int? resultat, int? activite, int? sousactivite)
    {
        string value = "VersMainPage?";
        if (resultat != null)
        {
            value += "resultat="+resultat;
        }

        if (activite != null)
        {
            value += "&activite=" + activite;
        }

        if (sousactivite != null)
        {
            value += "&sousactivite=" + sousactivite;
        }
        var tmp = Path.GetTempFileName();

        var req = HttpContext.Request;

        var uri = new Uri($"{req.Scheme}://{req.Host}{req.PathBase}/ExportPDF/"+value);
        
        var browserPath = BrowserFetcher.GetSystemChromePath();

        using var browser = new GcHtmlBrowser(browserPath);

        using var htmlPage = browser.NewPage(uri);

        PdfOptions pdfOptions = new PdfOptions()
        {
            PageRanges = "1-100",
            Margins = new PdfMargins(0.2f),
            Landscape = false,
            PreferCSSPageSize = true
        };

        htmlPage.SaveAsPdf(tmp, pdfOptions);
        var stream = new MemoryStream();
        using (var ts = System.IO.File.OpenRead(tmp))
            ts.CopyTo(stream);
        System.IO.File.Delete(tmp);
        return File(stream.ToArray(), MediaTypeNames.Application.Pdf, "document.pdf");
    }
    
    public IActionResult VersMainPage(int resultat, int budgetresultat, int activite, int budgetactivite, int sousactivite, int budgetsousactivite)
    {
    int idprojet = HttpContext.Session.GetInt32("idprojet").GetValueOrDefault();
    List<OccurenceResultat> listeResultats = new List<OccurenceResultat>();

    if (resultat == 1)
    {
        var resultatsFromDb = _context.OccurenceResultat
            .Where(a => a.IdProjet == idprojet)
            .ToList();

        foreach (var resultatDb in resultatsFromDb)
        {
            var resultatModel = new OccurenceResultat
            {
                Id = resultatDb.Id,
                IdProjet = resultatDb.IdProjet,
                IdResultat = resultatDb.IdResultat,
                Avancement = resultatDb.Avancement,
                ListeOccurenceActivites = new List<OccurenceActivite>()
            };

            if (activite == 1)
            {
                var activitesFromDb = _context.OccurenceActivite
                    .Where(a => a.IdOccurenceResultat == resultatDb.Id)
                    .ToList();

                foreach (var activiteDb in activitesFromDb)
                {
                    var activiteModel = new OccurenceActivite
                    {
                        Id = activiteDb.Id,
                        IdOccurenceResultat = activiteDb.IdOccurenceResultat,
                        IdActivite = activiteDb.IdActivite,
                        Budget = activiteDb.Budget,
                        DateDebut = activiteDb.DateDebut,
                        DateFin = activiteDb.DateFin,
                        Avancement = activiteDb.Avancement,
                        ListeOccurenceSousActivites = new List<OccurenceSousActivite>()
                    };

                    if (sousactivite == 1)
                    {
                        var sousActivitesFromDb = _context.OccurenceSousActivite
                            .Where(s => s.IdOccurenceActivite == activiteDb.Id)
                            .ToList();

                        foreach (var sousActiviteDb in sousActivitesFromDb)
                        {
                            var sousActiviteModel = new OccurenceSousActivite
                            {
                                Id = sousActiviteDb.Id,
                                IdOccurenceActivite = sousActiviteDb.IdOccurenceActivite,
                                IdSousActivite = sousActiviteDb.IdSousActivite,
                                Budget = sousActiviteDb.Budget,
                                DateDebut = sousActiviteDb.DateDebut,
                                DateFin = sousActiviteDb.DateFin,
                                Avancement = sousActiviteDb.Avancement
                            };

                            activiteModel.ListeOccurenceSousActivites.Add(sousActiviteModel);
                        }
                    }

                    resultatModel.ListeOccurenceActivites.Add(activiteModel);
                }
            }

            listeResultats.Add(resultatModel);
        }
    }
    return View("MainPage", listeResultats);
}

}