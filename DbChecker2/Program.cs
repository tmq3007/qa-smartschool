using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

class Program {
    static void Main() {
        QASmartClass.Services.AppPaths.Initialize();
        using var db = new AppDbContext();
        var rosters = db.ClassRosters.ToList();
        foreach (var r in rosters) {
            var links = db.ClassRosterStudents.Where(rs => rs.RosterId == r.Id).ToList();
            var studentIds = links.Select(rs => rs.StudentId).ToList();
            var students = db.Students.Where(s => studentIds.Contains(s.Id)).ToList();
            Console.WriteLine("Roster: " + r.ClassName + " (ID: " + r.Id + "), Links: " + links.Count + ", Students: " + students.Count);
        }
    }
}
