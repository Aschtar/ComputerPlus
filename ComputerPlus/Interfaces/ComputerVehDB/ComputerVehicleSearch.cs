﻿using System;
using System.Collections.Generic;
using System.Linq;
using Rage.Forms;
using Gwen;
using Gwen.Control;
using Rage;
using LSPD_First_Response.Engine.Scripting.Entities;
using ComputerPlus.Controllers.Models;
using ComputerPlus.Extensions.Gwen;
using System.Runtime.ExceptionServices;
using ComputerPlus.Controllers;

namespace ComputerPlus.Interfaces.ComputerVehDB
{
    sealed class ComputerVehicleSearch : GwenForm
    {
        ListBox list_collected_tags;
        ListBox list_manual_results;
        List<ComputerPlusEntity> AlprDetectedVehicles = new List<ComputerPlusEntity>();
        TextBox text_manual_name;

        internal ComputerVehicleSearch() : base(typeof(ComputerVehicleSearchTemplate))
        {

        }

        ~ComputerVehicleSearch()
        {
            list_manual_results.RowSelected -= OnListItemSelected;
            list_collected_tags.RowSelected -= OnListItemSelected;
            text_manual_name.SubmitPressed -= OnSearchSubmit;
        }

        public override void InitializeLayout()
        {
            base.InitializeLayout();
            this.Position = this.GetLaunchPosition();
            this.Window.DisableResizing();
            Function.LogDebug("Populating ALPR list");
            AlprDetectedVehicles.Clear();
            PopulateAnprList();
            list_collected_tags.AllowMultiSelect = false;
            list_manual_results.AllowMultiSelect = false;
            list_collected_tags.RowSelected += OnListItemSelected;
            list_manual_results.RowSelected += OnListItemSelected;
            text_manual_name.SubmitPressed += OnSearchSubmit;
            Function.LogDebug("Checking currently pulled over");
            var currentPullover = ComputerVehicleController.CurrentlyPulledOver;
            
            if (currentPullover != null && AlprDetectedVehicles.Find(x => x.Vehicle == currentPullover.Vehicle) == null)
            {
                AlprDetectedVehicles.Add(currentPullover);
            }
            foreach (var vehicle in AlprDetectedVehicles)
            {
                list_collected_tags.AddVehicle(vehicle);
                ComputerReportsController.generateRandomHistory(vehicle);
            }
        }

        private void ClearSelections()
        {
            list_collected_tags.UnselectAll();
            list_manual_results.UnselectAll();
        }

        private void OnSearchSubmit(Base sender, EventArgs arguments)
        {
            String tag = text_manual_name.Text.ToUpper();
            if (String.IsNullOrWhiteSpace(tag)) return;
            var vehicle = ComputerVehicleController.LookupVehicle(tag);
            
            
            if (vehicle != null && vehicle.Validate())
            {
                text_manual_name.ClearError();
                list_manual_results.AddVehicle(vehicle);
                ComputerVehicleController.LastSelected = vehicle;                
                this.ShowDetailsView();
            }
            else if(vehicle != null)
            {
                text_manual_name.Error("The vehicle no longer exists");
            }
            else
            {                
                text_manual_name.Error("No vehicles found");
            }
        }

        private void ShowDetailsView()
        {
            ComputerVehicleController.ShowVehicleDetails();
            this.Close();
        }

        private void OnListItemSelected(Base sender, ItemSelectedEventArgs arguments)
        {
            try
            { 
                if (arguments.SelectedItem.UserData is ComputerPlusEntity)
                {
                    ComputerVehicleController.LastSelected = arguments.SelectedItem.UserData as ComputerPlusEntity;
                    Function.AddVehicleToRecents(ComputerVehicleController.LastSelected.Vehicle);
                    ClearSelections();
                    this.ShowDetailsView();
                }
            }
            catch(Exception e)
            {
                Function.Log(e.ToString());
            }
        }
        

        private void PopulateAnprList()
        {
            try { 
                ComputerVehicleController.ALPR_Detected
                //.GroupBy(x => x.Vehicle)
                //.Select(x => x.Last())
                .Where(x => x.Vehicle)
                .GroupBy(x => x.Vehicle.LicensePlate)
                .Select(x => x.FirstOrDefault())
                .Select(x =>
                {
                    var data = ComputerVehicleController.LookupVehicle(x.Vehicle);
                
                    if (data == null)
                    {
                        Function.Log("ALPR integration issue.. data missing from LookupVehicle");
                        return null;
                    }
                    if (!String.IsNullOrWhiteSpace(x.Message))
                    {
                        //@TODO may have to come back to this
                        //vehiclePersona.Alert = x.Message;
                      //  data.VehiclePersona = vehiclePersona;
                    }
                
                    return data;
                })
                //.Where(x => x != null && x.Validate())
                .ToList()
                .ForEach(x =>
                {
                    AlprDetectedVehicles.Add(x);
                });
            }
            catch (Exception e)
            {
                Function.Log(e.ToString());
            }

        }
       
    }
}
