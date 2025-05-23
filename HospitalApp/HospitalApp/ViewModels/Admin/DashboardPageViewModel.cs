using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using HospitalApp.Models;
using HospitalApp.Services;
using HospitalApp.Helpers;
using Avalonia.Media.Imaging;

namespace HospitalApp.ViewModels
{
    public partial class DashboardPageViewModel : ViewModelBase
    {
        public Bitmap? DoctorImage { get; set; }
        public Bitmap? PatientImage { get; set; }
        private readonly ApiService _apiService;
        private readonly SignalRService _signalRService;

        [ObservableProperty]
        private ObservableCollection<Doctor> doctors = new();

        [ObservableProperty]
        private ObservableCollection<Patient> patients = new();

        [ObservableProperty]
        private Doctor? selectedDoctor;

        [ObservableProperty]
        private bool showDoctor;

        [ObservableProperty]
        private Patient? selectedPatient;

        [ObservableProperty]
        private bool showPatient;
        [ObservableProperty]
        private ObservableCollection<Doctor> filteredDoctors = new();

        [ObservableProperty]
        private string searchText = string.Empty;
        [ObservableProperty]
        private ObservableCollection<Patient> filteredPatients = new();

        [ObservableProperty]
        private string searchTextPatient = string.Empty;

        private readonly UserSessionService _session = UserSessionService.Instance;


        [ObservableProperty]
        private User _currentUser;

        [ObservableProperty]
        private int totalDoctors;

        [ObservableProperty]
        private int newDoctors;

        [ObservableProperty]
        private int availableDoctors;

        [ObservableProperty]
        private int totalPatients;

        [ObservableProperty]
        private int newPatients;

        [ObservableProperty]
        private string doctorImagePath;

        [ObservableProperty]
        private string patientImagePath;

        public AdminDashChartDocViewModel AdminChart {get;} = new();  
        public AdminDashChartPatientsViewModel AdminChartPatients {get;} = new();  

        // [Observable Property]

        // private Admin admin = 

        public DashboardPageViewModel(ApiService apiService, SignalRService signalRService)
        {
            _apiService = apiService;
            _signalRService = signalRService;

            _signalRService.DoctorAvailabilityUpdated += OnDoctorAvailabilityUpdated;
            _signalRService.DoctorAdded += OnDoctorAdded;
            
            _currentUser = _session.CurrentUser;

            LoadDoctors();
            LoadPatients();
            ConnectToSignalR();
            Console.WriteLine($"CurrentUser: {_currentUser?.Name}");

        }

        private async void ConnectToSignalR()
        {
            await _signalRService.ConnectAsync();
        }

        private async void LoadDoctors()
        {
            var doctorsList = await _apiService.GetDoctorsAsync();
            Doctors = new ObservableCollection<Doctor>(doctorsList);
            var total = await _apiService.GetTotalPatients();
            TotalPatients = total;
            var newpatients = await _apiService.GetNewPatients(DateTime.Now.Year, DateTime.Now.Month);
            NewPatients = newpatients;
            AvailableDoctors = Doctors.Count(d => d.is_available == 1);
            FilterDoctors();
        }

        private async void LoadPatients()
        {
            var patientsList = await _apiService.GetPatientsAsync();
            Patients = new ObservableCollection<Patient>(patientsList);
            var total = await _apiService.GetTotalDoctors();
            TotalDoctors = total;
            var newDoc = await _apiService.GetNewDoctors(DateTime.Now.Year, DateTime.Now.Month);
            NewDoctors = newDoc;
            AvailableDoctors = Doctors.Count(d => d.is_available == 1);
            FilterPatients(); // Ensure this line is present
        }

        private void OnDoctorAvailabilityUpdated(int doctorId, int isAvailable)
        {
            var doctor = Doctors.FirstOrDefault(d => d.Id == doctorId);
            if (doctor != null)
            {
                doctor.is_available = isAvailable;
                FilterDoctors();
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterDoctors();
        }

        partial void OnSearchTextPatientChanged(string value)
        {
            FilterPatients();
        }


        private void FilterDoctors()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredDoctors = new ObservableCollection<Doctor>(
                    Doctors.OrderByDescending(d => d.is_available)
                );
            }
            else
            {
                var filteredList = Doctors
                    .Where(d => d.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(d => d.is_available)
                    .ToList();

                FilteredDoctors = new ObservableCollection<Doctor>(filteredList);
            }
        }
        private void OnDoctorAdded(Doctor doctor)
        {
            if (!Doctors.Any(d => d.Id == doctor.Id)) // Prevent duplicate entries
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Doctors.Add(doctor);
                    FilterDoctors(); // Refresh filtered list
                });
            }
        }

        partial void OnSelectedDoctorChanged(Doctor? value)
        {
            ShowDoctor = value != null;
            Console.WriteLine($"SelectedDoctor: {value.profile_picture}");
            DoctorImage = ImageHelper.LoadFromResource(new Uri(value.profile_picture));
        }

        partial void OnSelectedPatientChanged(Patient? value)
        {
            ShowPatient = value != null;
            Console.WriteLine($"SelectedPatient: {value.ProfilePicture}");
            PatientImage = ImageHelper.LoadFromResource(new Uri(value.ProfilePicture));
        }

        private void FilterPatients()
        {
            if (string.IsNullOrWhiteSpace(SearchTextPatient))
            {
                FilteredPatients = new ObservableCollection<Patient>(Patients); // ✅
            }
            else
            {
                var filteredList = Patients
                    .Where(p => p.Name.Contains(SearchTextPatient, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                FilteredPatients = new ObservableCollection<Patient>(filteredList); // ✅
            }
        }

    }
}
