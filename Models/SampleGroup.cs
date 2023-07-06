using Livet;
using System.Collections.Generic;
using System.Linq;

namespace ShotgunMetagenome.Models
{
    public class SampleGroup : NotificationObject
    {

        public int Id { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public ColorModel Color { get; set; }


        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }


        // file path.
        public IEnumerable<SampleGroup> Sequences { get; set; } = new List<SampleGroup>();
        public IEnumerable<string> FilePaths =>
                                                this.Sequences.Select(s => s.Name);

         
        public void SetFilePaths(IEnumerable<string> filePaths)
        {
            var cnt = 0;
            var seqs = new List<SampleGroup>();
            foreach( var path in filePaths)
            {
                seqs.Add(new SampleGroup()
                {
                    Id = ++cnt,
                    Name = path,
                    Description = System.IO.Path.GetFileName(path)
                });
            }
            this.Sequences = seqs;

        }

        public void AddFilePath(IEnumerable<string> filePaths)
        {
            var seqs = Sequences.ToList();
            var cnt = Sequences.Count();
            foreach (var path in filePaths)
            {
                if (Sequences.Where(s => s.Name == path).Any())
                    continue;

                seqs.Add(new SampleGroup()
                {
                    Id = ++cnt,
                    Name = path,
                    Description = System.IO.Path.GetFileName(path)
                });
            }
            Sequences = seqs;
            RaisePropertyChanged(nameof(Sequences));
        }
    }

}
