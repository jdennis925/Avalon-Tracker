﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class AvalonModelContainer : DbContext
    {
        public AvalonModelContainer()
            : base("name=AvalonModelContainer")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<ActivePlayer> ActivePlayers { get; set; }
        public virtual DbSet<Party> Parties { get; set; }
        public virtual DbSet<Quest> Quests { get; set; }
        public virtual DbSet<Game> Games { get; set; }
        public virtual DbSet<QuestVote> QuestVotes { get; set; }
        public virtual DbSet<PartyVote> PartyVotes { get; set; }
        public virtual DbSet<Player> Players { get; set; }
        public virtual DbSet<Character> Characters { get; set; }
        public virtual DbSet<RevealedCharacter> RevealedCharacters { get; set; }
    }
}
