using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class UtakmicaController : Controller
    {
        // GET: Utakmica
        public ActionResult Index()
        {
            // Vraća Utakmica/Index
            return View();
        }

        // Add se poziva iz Index
        [HttpPost]
        public ActionResult Add(Utakmica utakmica)
        {
            // Ovo je inicijalizovano u Global.asax.cs
            List<Utakmica> lista = (List<Utakmica>)HttpContext.Application["utakmice"];
            
            if (lista.Where(x => x.ID == utakmica.ID).Count() > 0)
            {
                Utakmica u = lista.First();
                foreach (Utakmica u1 in lista) if (u1.ID == utakmica.ID) u = u1;
                return View("DupliIndeks", u);
            }
            
            lista.Add(utakmica);
            return View("Tabela", lista);
        }

        // Delete se poziva iz Tabela
        public ActionResult Delete(int id)
        {
            // Ovo je inicijalizovano u Global.asax.cs
            List<Utakmica> lista = (List<Utakmica>)HttpContext.Application["utakmice"];
            lista.Remove(lista.Where(x => x.ID == id).ElementAt(0));
            return View("Tabela", lista);
        }

        // Filter se poziva iz Index
        public ActionResult Filter(string Liga)
        {
            List<Utakmica> lista = (List<Utakmica>)HttpContext.Application["utakmice"];
            return View("Tabela", lista.Where(x => x.Liga == Liga));
        }

        // Edit se poziva iz Tabela
        public ActionResult Edit(int id)
        {
            // Ovo je inicijalizovano u Global.asax.cs
            List<Utakmica> lista = (List<Utakmica>)HttpContext.Application["utakmice"];
            return View("Edit", lista.Where(x => x.ID == id).ElementAt(0));
        }

        // Update se poziva iz Edit
        [HttpPost]
        public ActionResult Update(Utakmica u)
        {
            // Ovo je inicijalizovano u Global.asax.cs
            List<Utakmica> lista = (List<Utakmica>)HttpContext.Application["utakmice"];
            
            foreach (var x in lista)
            {
                if (x.ID == u.ID)
                {
                    x.Gost = u.Gost;
                    x.Domacin = u.Domacin;
                    x.Liga = u.Liga;
                    x.Pobednik = u.Pobednik;
                    break;
                }
            }

            return View("Tabela", lista);
        }
    }
}
