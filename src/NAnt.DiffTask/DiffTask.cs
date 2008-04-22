using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

using NAnt.Core;
using NAnt.Core.Util;
using NAnt.Core.Types;
using NAnt.Core.Attributes;

using DifferenceEngine;



namespace Poiesis.NAnt.DiffTasks
{
    [TaskName("diff")]
    public class Diff : Task
    {

        private string _source;
        [TaskAttribute("source", Required = true)]
        public string Source
        {
            get { return _source; }
            set { _source = value; }
        }

        private string _destination;
        [TaskAttribute("destination", Required = true)]
        public string Destination
        {
            get { return _destination; }
            set { _destination = value; }
        }

        private string _merge;
        [TaskAttribute("merge")]
        public string Merge
        {
            get { return _merge; }
            set { _merge = value; }
        }

        private string _tokenNew = "/*N*/";
        [TaskAttribute("tokennew")]
        public string TokenNew
        {
            get { return _tokenNew; }
            set { _tokenNew = value; }
        }

        private string _tokenReplace = "/*R*/";
        [TaskAttribute("tokennew")]
        public string TokenReplace
        {
            get { return _tokenReplace; }
            set { _tokenReplace = value; }
        }

        private bool _binary;
        [TaskAttribute("binary")]
        public bool Binary
        {
            get { return _binary; }
            set { _binary = value; }
        }

        private DiffEngineLevel _engineLevel = DiffEngineLevel.SlowPerfect;
        [TaskAttribute("level")]
        public DiffEngineLevel EngineLevel
        {
            get { return _engineLevel; }
            set { _engineLevel = value; }
        }

        double _time;

        protected override void ExecuteTask()
        {
            bool doMerge = (String.Empty != _merge);

            if (ValidFile(_source) && ValidFile(_destination))
            {
                if (this._binary)
                    BinaryDiff(_source, _destination, doMerge);

                else
                    TextDiff(_source, _destination, doMerge);
            }
            Log(Level.Info, "Compare finished in '{0}' seconds.", _time.ToString("#0.00"));
             
        }

        private ArrayList TextDiff(string sFile, string dFile, bool merge)
        {
            DiffList_TextFile sLF = null;
            DiffList_TextFile dLF = null;
            ArrayList rep = new ArrayList();

            try
            {
                sLF = new DiffList_TextFile(sFile);
                dLF = new DiffList_TextFile(dFile);
            }
            catch (Exception ex)
            {

                System.Console.WriteLine("File Error");
                System.Console.WriteLine(ex.Message);
                return rep;
            }

            try
            {
                _time = 0;
                DiffEngine de = new DiffEngine();
                _time = de.ProcessDiff(sLF, dLF, _engineLevel);

                rep = de.DiffReport();
                PrintReport(rep);
                if (merge)
                {
                    MergeFiles(sLF, dLF, rep);
                }
                return rep;
            }
            catch (Exception ex)
            {

                string tmp = string.Format("{0}{1}{1}***STACK***{1}{2}",
                ex.Message,
                Environment.NewLine,
                ex.StackTrace);
                System.Console.WriteLine("Compare Error");
                System.Console.WriteLine(tmp);
                return rep;
            }

        }

        private ArrayList BinaryDiff(string sFile, string dFile, bool merge)
        {
            DiffList_BinaryFile sLF = null;
            DiffList_BinaryFile dLF = null;
            ArrayList rep = new ArrayList();

            try
            {
                sLF = new DiffList_BinaryFile(sFile);
                dLF = new DiffList_BinaryFile(dFile);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("File Error");
                System.Console.WriteLine(ex.Message);
                return rep;
            }

            try
            {
                _time = 0;
                DiffEngine de = new DiffEngine();
                _time = de.ProcessDiff(sLF, dLF, _engineLevel);

                rep = de.DiffReport();
                PrintReport(rep);
                if (merge)
                {
                    //MergeFiles(sLF, dLF, rep);
                }
                return rep;
            }
            catch (Exception ex)
            {

                string tmp = string.Format("{0}{1}{1}***STACK***{1}{2}",
                    ex.Message,
                    Environment.NewLine,
                    ex.StackTrace);
                System.Console.WriteLine("Compare Error");
                System.Console.WriteLine(tmp);
                return rep;
            }

        }

        public void MergeFiles(DiffList_TextFile source, DiffList_TextFile destination, ArrayList DiffLines)
        {

            try
            {
                FileInfo fileMerge = new FileInfo(this._merge);
                StreamWriter writer = fileMerge.CreateText();

                string output = "";
                int cnt = 1;
                int i;

                foreach (DiffResultSpan drs in DiffLines)
                {
                    switch (drs.Status)
                    {
                        //DeleteSource: significa todas las líneas agregadas en el source
                        //accion agregar esas línea en outpút final.
                        case DiffResultSpanStatus.DeleteSource:
                            for (i = 0; i < drs.Length; i++)
                            {
                                writer.WriteLine(((TextLine)source.GetByIndex(drs.SourceIndex + i)).Line);
                                cnt++;
                            }
                            break;

                        //No Change: Sin cambios se imprime     
                        case DiffResultSpanStatus.NoChange:
                            for (i = 0; i < drs.Length; i++)
                            {
                                writer.WriteLine(((TextLine)source.GetByIndex(drs.SourceIndex + i)).Line);
                                cnt++;
                            }
                            break;

                        //AddDestination: significa las líneas que ha agregado el template y que no
                        //se han reemplazado en el source
                        case DiffResultSpanStatus.AddDestination:
                            for (i = 0; i < drs.Length; i++)
                            {
                                writer.WriteLine((((TextLine)destination.GetByIndex(drs.DestIndex + i))).Line);
                                cnt++;
                            }

                            break;

                        //Replace:
                        case DiffResultSpanStatus.Replace:

                            List<int> addLines = new List<int>();
                            List<int> replaceLines = new List<int>();

                            for (i = 0; i < drs.Length; i++)
                            {
                                output = ((TextLine)source.GetByIndex(drs.SourceIndex + i)).Line;
                                //Si las lineas estan marcadas como nuevas
                                if (output.StartsWith(_tokenNew))
                                {
                                    writer.WriteLine(output);
                                    addLines.Add(drs.SourceIndex + i);
                                }

                                else if (output.StartsWith(_tokenReplace))
                                {
                                    writer.WriteLine(output);
                                    replaceLines.Add(drs.SourceIndex + i);
                                }

                                else
                                {
                                    addLines.Add(drs.SourceIndex + i);
                                }
                                cnt++;
                            }

                            for (i = 0; i < addLines.Count; i++)
                            {
                                output = ((TextLine)destination.GetByIndex(addLines[i])).Line;
                                writer.WriteLine(output);
                                cnt++;
                            }
                            break;
                    }
                }
                writer.Flush();
                writer.Close();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private bool ValidFile(string fname)
        {
            if (fname != string.Empty)
            {
                if (File.Exists(fname))
                {
                    return true;
                }
            }
            return false;
        }

        private void PrintReport(ArrayList results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                Log(Level.Info, results[i].ToString());

            }
        }
    }
}
/*
    //Genera un modelo a partir de un assembly
    [TaskName("fillmodel")]
    public class FillModel : Task
    {
        private string _pathAssembly;
        [TaskAttribute("assembly", Required = true)]
        public string PathAssembly
        {
            get { return _pathAssembly; }
            set { _pathAssembly = value; }
        }

        private string _templateID;
        [TaskAttribute("templateid")]
        public string TemplateID
        {
            get { return _templateID; }
            set { _templateID = value; }
        }

        private string _templateDescription;
        [TaskAttribute("templatedescription")]
        public string TemplateDescription
        {
            get { return _templateDescription; }
            set { _templateDescription = value; }
        }

        private AssemblyFileSet _domainAssemblies = new AssemblyFileSet();
        [BuildElement("assemblies", Required = false)]
        public AssemblyFileSet DomainAssemblies
        {
            get { return _domainAssemblies; }
            set { _domainAssemblies = value; }
        }


        private string _toFile = "";

        [TaskAttribute("exportTo", Required = false)]
        public string ToFile
        {
            get { return _toFile; }
            set { _toFile = value; }
        }

        private string _database = "";

        [TaskAttribute("database", Required = false)]
        public string Database
        {
            get { return _database; }
            set { _database = value; }
        }

        protected override void ExecuteTask()
        {
            try
            {
                ModelInfo newModel = AppModel.FillModelFromAssembly(_pathAssembly, false);
                newModel.RegExForID = this._templateID;
                newModel.RegExForNameOrDescription = this._templateDescription;

                if (_toFile != "")
                {
                    newModel.ToXml().Save(_toFile);
                    System.Console.WriteLine("Modelo exportado a XML");

                }

                if (_database != "")
                {
                    AppModel.SaveModel(_database, newModel);
                    System.Console.WriteLine("Modelo guardado en db");
                }


            }
            catch (System.Exception ex)
            {
                throw ex;
            }

        }

    }


    [TaskName("exportmodel")]
    public class ExportModel : Task
    {

        private string _dbPath;

        [TaskAttribute("dbPath", Required = true)]
        public string dbPath
        {
            get { return _dbPath; }
            set { _dbPath = value; }
        }

        private string _toFile;

        [TaskAttribute("toFile", Required = true)]
        public string ToFile
        {
            get { return _toFile; }
            set { _toFile = value; }
        }


        private int _indexDBModel = 0;

        [TaskAttribute("indexDbModel", Required = false)]
        public int IndexDBModel
        {
            get { return _indexDBModel; }
            set { _indexDBModel = value; }
        }

        protected override void ExecuteTask()
        {
            ModelInfo modelSaved = AppModel.GetModelSaved(this.dbPath, _indexDBModel);
            modelSaved.ToXml().Save(_toFile);
            AppModel.CloseDbModel();


        }

    }
}*/