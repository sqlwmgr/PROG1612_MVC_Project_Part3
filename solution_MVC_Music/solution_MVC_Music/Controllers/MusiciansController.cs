using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using solution_MVC_Music.Data;
using solution_MVC_Music.Models;
using solution_MVC_Music.ViewModels;

namespace solution_MVC_Music.Controllers
{
    public class MusiciansController : Controller
    {
        private readonly MusicContext _context;

        public MusiciansController(MusicContext context)
        {
            _context = context;
        }

        // GET: Musicians
        public async Task<IActionResult> Index(int? InstrumentID, string SearchString, string sortDirection, string sortField, string actionButton)
        {
            ViewData["InstrumentID"] = new SelectList(_context.Instruments.OrderBy(i => i.Name), "ID", "Name");
            PopulateDropDownLists();
            ViewData["Filtering"] = "";

            //var musicContext = _context.Musicians
            //    .Include(m => m.Instrument)
            //    .Include(m=>m.Plays).ThenInclude(p=>p.Instrument);

            var musicians = from m in _context.Musicians
                .Include(m => m.Instrument)
                .Include(m => m.Plays)
                .ThenInclude(p => p.Instrument)
                select m;

            if (InstrumentID.HasValue)
            {
                musicians = musicians.Where(m => m.InstrumentID == InstrumentID);
                ViewData["Filtering"] = "in";
            }
            if (!string.IsNullOrEmpty(SearchString))
            {
                musicians = musicians.Where(m => m.LastName.ToUpper().Contains(SearchString.ToUpper())||m.FirstName.ToUpper().Contains(SearchString.ToUpper()));
            }

            if (!string.IsNullOrEmpty(actionButton))
            {
                if (actionButton != "Filter")
                {
                    if (actionButton == sortField)
                    {
                        sortDirection = string.IsNullOrEmpty(sortDirection) ? "desc" : "";
                    }
                    sortField = actionButton;
                }
            }

            if (sortField == "Musician")
            {
                if (string.IsNullOrEmpty(sortDirection))
                {
                    musicians = musicians
                        .OrderBy(m => m.FirstName)
                        .ThenBy(m => m.LastName);
                }
                else
                {
                    musicians = musicians
                        .OrderByDescending(m => m.FirstName)
                        .ThenByDescending(m => m.LastName);
                }
            }
            else if (sortField == "Phone")
            {
                if (string.IsNullOrEmpty(sortDirection))
                {
                    musicians = musicians
                        .OrderBy(m => m.Phone);
                }
                else
                {
                    musicians = musicians
                        .OrderByDescending(m => m.Phone);
                }
            }
            else if (sortField == "Age")
            {
                if (string.IsNullOrEmpty(sortDirection))
                {
                    musicians = musicians
                        .OrderBy(m => m.DOB);
                }
                else
                {
                    musicians = musicians
                        .OrderByDescending(m => m.DOB);
                }
            }
            else if (sortField == "Instrument")
            {
                if (string.IsNullOrEmpty(sortDirection))
                {
                    musicians = musicians
                        .OrderBy(m => m.Instrument.Name);
                }
                else
                {
                    musicians = musicians
                        .OrderByDescending(m => m.Instrument.Name);
                }
            }

            ViewData["sortField"] = sortField;
            ViewData["sortDirection"] = sortDirection;

            return View(await musicians.ToListAsync());
        }

        // GET: Musicians/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var musician = await _context.Musicians
                .Include(m => m.Instrument)
                .Include(m => m.Plays).ThenInclude(p => p.Instrument)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (musician == null)
            {
                return NotFound();
            }

            return View(musician);
        }

        // GET: Musicians/Create
        public IActionResult Create()
        {
            var musician = new Musician();
            musician.Plays = new List<Plays>();
            PopulateAssignedInstrumentData(musician);

            PopulateDropDownLists();
            return View();
        }

        // POST: Musicians/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,FirstName,MiddleName,LastName,Phone,DOB,SIN,InstrumentID")] Musician musician, string[] selectedInstruments)
        {
            try
            {
                if (selectedInstruments != null)
                {
                    musician.Plays = new List<Plays>();
                    foreach(var ins in selectedInstruments)
                    {
                        var insToAdd = new Plays { MusicianID = musician.ID, InstrumentID = int.Parse(ins) };
                        musician.Plays.Add(insToAdd);
                    }
                }

                if (ModelState.IsValid)
                {
                    _context.Add(musician);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (RetryLimitExceededException)
            {
                ModelState.AddModelError("", "Unable to save changes after multiple attempts. Try again,");
            }
            catch (DbUpdateException dex)
            {
                if (dex.InnerException.Message.Contains("IX_Musicians_SIN"))
                {
                    ModelState.AddModelError("", "Unable to save changes. Remember, you cannot have duplicate SIN numbers.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }

            PopulateDropDownLists(musician);
            return View(musician);
        }

        // GET: Musicians/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var musician = await _context.Musicians
                .Include(m => m.Instrument)
                .Include(m => m.Plays).ThenInclude(p => p.Instrument)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);

            if (musician == null)
            {
                return NotFound();
            }
            PopulateDropDownLists(musician);
            PopulateAssignedInstrumentData(musician);
            return View(musician);
        }

        // POST: Musicians/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string[] selectedInstruments)//, [Bind("ID,FirstName,MiddleName,LastName,Phone,DOB,SIN,InstrumentID")] Musician musician)
        {
            var musicianToUpdate = await _context.Musicians
                .Include(m => m.Instrument)
                .Include(m => m.Plays).ThenInclude(p => p.Instrument)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (musicianToUpdate == null)
            {
                return NotFound();
            }
            UpdateMusicianInstruments(selectedInstruments, musicianToUpdate);

            //Try updating it with the values posted
            if (await TryUpdateModelAsync<Musician>(musicianToUpdate, "",
                p => p.SIN, p => p.FirstName, p => p.MiddleName, p => p.LastName, p => p.DOB,
                p => p.Phone, p => p.InstrumentID))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MusicianExists(musicianToUpdate.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException dex)
                {
                    if (dex.InnerException.Message.Contains("IX_Musicians_SIN"))
                    {
                        ModelState.AddModelError("", "Unable to save changes. Remember, you cannot have duplicate SIN numbers.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                    }
                }
            }

            PopulateDropDownLists(musicianToUpdate);
            return View(musicianToUpdate);
        }

        // GET: Musicians/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var musician = await _context.Musicians
                .Include(m => m.Instrument)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == id);
            if (musician == null)
            {
                return NotFound();
            }

            return View(musician);
        }

        // POST: Musicians/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var musician = await _context.Musicians
                .Include(m => m.Instrument)
                .FirstOrDefaultAsync(m => m.ID == id);
            try
            {
                _context.Musicians.Remove(musician);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dex)
            {
                if (dex.InnerException.Message.Contains("FK_Performances_Musicians_MusicianID"))
                {
                    ModelState.AddModelError("", "Unable to save changes. You cannot delete a Musician who performed on any songs.");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
                }
            }
            return View(musician);
        }

        //This is a twist on the PopulateDropDownLists approach
        //  Create methods that return each SelectList separately
        //  and one method to put them all into ViewData.
        //This approach allows for AJAX requests to refresh
        //DDL Data at a later date.
        private SelectList InstrumentSelectList(Musician musician = null)
        {
            var dQuery = from i in _context.Instruments
                         orderby i.Name
                         select i;
            return new SelectList(dQuery, "ID", "Name", musician?.InstrumentID);
        }

        private void PopulateAssignedInstrumentData(Musician musician)
        {
            var allInstruments = _context.Instruments;
            var pInstruments = new HashSet<int>(musician.Plays.Select(i => i.InstrumentID));
            var viewModel = new List<PlaysVM>();
            foreach(var ins in allInstruments)
            {
                viewModel.Add(new PlaysVM
                {
                    InstrumentID = ins.ID,
                    InstrumentName = ins.Name,
                    Assigned = pInstruments.Contains(ins.ID)
                });
            }
            ViewData["Instruments"] = viewModel;
        }

        private void PopulateDropDownLists(Musician musician = null)
        {
            ViewData["InstrumentID"] = InstrumentSelectList(musician);
        }

        private bool MusicianExists(int id)
        {
            return _context.Musicians.Any(e => e.ID == id);
        }

        private void UpdateMusicianInstruments(string[] selectedInstruments, Musician musicianToUpdate)
        {
            if (selectedInstruments == null)
            {
                musicianToUpdate.Plays = new List<Plays>();
                return;
            }

            var selectedInstrumentsHS = new HashSet<string>(selectedInstruments);
            var insts = new HashSet<int>(musicianToUpdate.Plays.Select(p => p.InstrumentID));
            foreach(var inst in _context.Instruments)
            {
                if (selectedInstrumentsHS.Contains(inst.ID.ToString()))
                {
                    if (!insts.Contains(inst.ID))
                    {
                        musicianToUpdate.Plays.Add(new Plays { MusicianID = musicianToUpdate.ID, InstrumentID = inst.ID });
                    }
                }
                else
                {
                    if (insts.Contains(inst.ID))
                    {
                        Plays instrumentToRemove = musicianToUpdate.Plays.SingleOrDefault(p => p.InstrumentID == inst.ID);
                        _context.Remove(instrumentToRemove);
                    }
                }
            }
        }
    }
}
