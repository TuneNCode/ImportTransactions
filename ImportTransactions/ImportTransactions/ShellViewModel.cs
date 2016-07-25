using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ImportTransactions.Models;
using Microsoft.Win32;

namespace ImportTransactions
{
    public class ShellViewModel : Caliburn.Micro.PropertyChangedBase, IShell
    {
        private OpenFileDialog _ofd;

        private string _selectedFileName;

        private List<string> _transactionLines;

        private List<string> _invalidTransactions;

        private List<TransactionLine> _validTransactions;

        private bool _canImport;

        private decimal _percentageComplete;
        private int _transactionLinesCount;
        private int _invalidTransactionsCount;
        private int _validTransactionsCount;
        public ShellViewModel()
        {
            Exceptions = new List<Exception>();
        }
        public string SelectedFileName
        {
            get { return _selectedFileName; }
            set
            {
                if (value != null && value == _selectedFileName) return;
                _selectedFileName = value;
                NotifyOfPropertyChange();
                CanImport = !string.IsNullOrWhiteSpace(value);
            }
        }

        public List<string> TransactionLines
        {
            get { return _transactionLines; }
            set
            {
                if (Equals(value, _transactionLines)) return;
                _transactionLines = value;
                NotifyOfPropertyChange(() => TransactionLines);
                TransactionLinesCount = _transactionLines.Count;
                UpdateProgressIndicator();

            }
        }

        public int TransactionLinesCount
        {
            get { return _transactionLinesCount; }
            set
            {
                if (value == _transactionLinesCount) return;
                _transactionLinesCount = value;
                NotifyOfPropertyChange(() => TransactionLinesCount);
            }
        }

        public List<string> InvalidTransactions
        {
            get { return _invalidTransactions; }
            set
            {
                if (Equals(value, _invalidTransactions)) return;
                _invalidTransactions = value;
                NotifyOfPropertyChange(() => InvalidTransactions);
                InvalidTransactionsCount = _invalidTransactions.Count;
                UpdateProgressIndicator();

            }
        }

        public int InvalidTransactionsCount
        {
            get { return _invalidTransactionsCount; }
            set
            {
                if (value == _invalidTransactionsCount) return;
                _invalidTransactionsCount = value;
                NotifyOfPropertyChange(() => InvalidTransactionsCount);
            }
        }

        public List<TransactionLine> ValidTransactions
        {
            get { return _validTransactions; }
            set
            {
                if (Equals(value, _validTransactions)) return;
                _validTransactions = value;
                NotifyOfPropertyChange(() => ValidTransactions);
                UpdateProgressIndicator();
            }
        }

        public int ValidTransactionsCount
        {
            get { return _validTransactionsCount; }
            set
            {
                if (value == _validTransactionsCount) return;
                _validTransactionsCount = value;
                NotifyOfPropertyChange(() => ValidTransactionsCount);
            }
        }

        public decimal PercentageComplete
        {
            get { return _percentageComplete; }
            set
            {
                if (value == _percentageComplete) return;
                _percentageComplete = value;
                NotifyOfPropertyChange(() => PercentageComplete);
            }
        }

        public bool CanImport
        {
            get { return _canImport; }
            set
            {
                if (value == _canImport) return;
                _canImport = value;
                NotifyOfPropertyChange(() => CanImport);
            }
        }

        public void Browse()
        {
            _ofd = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "csv|*.csv",
                DefaultExt = ".csv",
                FileName = SelectedFileName,
                Title = "Select a transactions file for import",
                InitialDirectory = Directory.GetLogicalDrives().First()
            };
            if (_ofd.ShowDialog().GetValueOrDefault())
            {
                SelectedFileName = _ofd.FileName;
            }
        }

        public async void Import()
        {
            CanImport = false;
            await StartImport().ContinueWith(x =>
            {
                // Waiting for PercentageComplete to be updated
                Task.Delay(1000);

                CanImport = true;
                UpdateInvalidTransactionsList();
                UpdateProgressIndicator();
            });


        }

        private async Task StartImport()
        {
            try
            {
                TransactionLines = File.ReadLines(SelectedFileName).ToList();
                ValidTransactions = new List<TransactionLine>();
                InvalidTransactions = new List<string>();
                var transactionChunk = TransactionLines;
                await Task.Run(() =>
                {
                    transactionChunk.AsParallel().ForAll(ValidateTransactions);
                })
                ;

            }

            catch (Exception e)
            {

                Exceptions.Add(e);
            }
            finally
            {

                if (Exceptions.Any())
                {
                    InsertErrors();
                    MessageBox.Show(string.Format("{0} Exceptions.\n{1}", Exceptions.Count,
                        Exceptions.FirstOrDefault()?.Message));
                }
            }
        }

        public ObservableCollection<string> FailedLines { get; set; }

        private void UpdateInvalidTransactionsList()
        {
            FailedLines = new ObservableCollection<string>(InvalidTransactions);   
            NotifyOfPropertyChange(()=>FailedLines);
        }

        private void InsertValidTransactions(string values)
        {
            try
            {
                TransactionDataAccess.InsertTransactions(values);
            }
            catch (SqlException sqlException)
            {
                Exceptions.Add(sqlException);
            }
        }

        public List<Exception> Exceptions { get; set; }

        private void ValidateTransactions(string transactionLineString)
        {
            var separator = new[] { ',' };
            var columns = transactionLineString.Split(separator, 4, StringSplitOptions.None).ToList();
            if (columns.Count != 4)
            {
                InvalidTransactions.Add(transactionLineString);
                InsertInvalidTransactions(transactionLineString);
                UpdateProgressIndicator();
                return;
            }
            else
            {
                string account = columns[0].Trim();
                string description = columns[1].Trim();
                string currencyCode = columns[2].Trim();
                var isValidCode = IsValidCurrencyCode(currencyCode);
                decimal value;
                var isDecimal = decimal.TryParse(columns[3].Trim(), out value);

                if (isValidCode && isDecimal && !string.IsNullOrWhiteSpace(account) &&
                    !string.IsNullOrWhiteSpace(description))
                {
                    var transactionLine = new TransactionLine(account, description, currencyCode, value);
                    ValidTransactions.Add(transactionLine);
                    InsertValidTransactions(transactionLine.ToString());

                }
                else
                {
                    InvalidTransactions.Add(transactionLineString);
                    InsertInvalidTransactions(transactionLineString);
                }
            }

            UpdateProgressIndicator();
        }

        private void InsertInvalidTransactions(string transactionLineString)
        {
            try
            {
                TransactionDataAccess.InsertInvalidLines(transactionLineString);
            }
            catch (SqlException sqlException)
            {
                Exceptions.Add(sqlException);
            }
        }

        private void InsertErrors()
        {
            
            try
            {
                TransactionDataAccess.InsertErrors(Exceptions);
            }
            catch (SqlException sqlException)
            {
                Exceptions.Add(sqlException);
            }
        }


        private bool IsValidCurrencyCode(string currencyCode)
        {
            var symbol = CultureInfo
                .GetCultures(CultureTypes.AllCultures)
                .Where(c => !c.IsNeutralCulture)
                .Select(culture =>
                {
                    try
                    {
                        return new RegionInfo(culture.LCID);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(ri => ri != null && ri.ISOCurrencySymbol == currencyCode)
                .Select(ri => ri.CurrencySymbol)
                .FirstOrDefault();
            return symbol != null;
        }

        private void UpdateProgressIndicator()
        {
            try
            {
                ValidTransactionsCount = ValidTransactions != null ? ValidTransactions.Count : 0;
                InvalidTransactionsCount = InvalidTransactions != null ? InvalidTransactions.Count : 0;
                TransactionLinesCount = TransactionLines != null ? TransactionLines.Count : 0;
                var percentageComplete = 100 * (ValidTransactionsCount + InvalidTransactionsCount) / TransactionLinesCount;
                PercentageComplete = percentageComplete;
            }
            catch
            {
                PercentageComplete = 0;
            }
        }
    }
}