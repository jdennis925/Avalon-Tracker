//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AvalonTracker
{
    using System;
    using System.Collections.Generic;
    
    public partial class Party
    {
        public int Id { get; set; }
        public int PartyLeaderId { get; set; }
    
        public virtual Quest Quests { get; set; }
        public virtual GameInstance Game { get; set; }
    }
}
