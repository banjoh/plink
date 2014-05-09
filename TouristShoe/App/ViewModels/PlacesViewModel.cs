using System;
using System.ComponentModel;
using Microsoft.Phone.Maps.Services;

namespace App.ViewModels
{
    public class PlacesViewModel : INotifyPropertyChanged
    {
        public enum VisitStatus
        {
            Visit,
            DontVisit
        }

        private string _lineOne;
        public string LineOne
        {
            get
            {
                return _lineOne;
            }
            set
            {
                if (value == _lineOne) return;
                _lineOne = value;
                NotifyPropertyChanged("LineOne");
            }
        }

        private string _lineTwo;
        public string LineTwo
        {
            get
            {
                return _lineTwo;
            }
            set
            {
                if (value == _lineTwo) return;
                _lineTwo = value;
                NotifyPropertyChanged("LineTwo");
            }
        }

        private VisitStatus _visitStatusProperty;
        public VisitStatus VisitStatusProperty
        {
            get
            {
                return _visitStatusProperty;
            }
            set
            {
                if (value == _visitStatusProperty) return;
                _visitStatusProperty = value;
                NotifyPropertyChanged("VisitStatusProperty");
            }
        }

        private MapLocation _location;
        public MapLocation Location
        {
            get
            {
                return _location;
            }
            set
            {
                if (value == _location) return;
                _location = value;
                NotifyPropertyChanged("Location");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            var handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _largeImage;
        public string LargeImage
        {
            get
            {
                return _largeImage;
            }
            set
            {
                if (value == _largeImage) return;
                _largeImage = value;
                NotifyPropertyChanged("LargeImage");
            }
        }

        private string _thumbNail;
        public string ThumbNail
        {
            get
            {
                return _thumbNail;
            }
            set
            {
                if (value == _thumbNail) return;
                _thumbNail = value;
                NotifyPropertyChanged("ThumbNail");
            }
        }
    }
}