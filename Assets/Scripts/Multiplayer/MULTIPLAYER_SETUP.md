# Multiplayer Setup Guide

Complete guide to set up Unity Multiplayer with Mystery Rooms backend integration.

---

## 📦 Prerequisites

### 1. Unity Packages (Already Installed ✅)
- `com.unity.services.multiplayer: 2.3.1`
- `com.unity.netcode.gameobjects: 2.13.1`
- `com.unity.services.core: 1.18.0`

### 2. Unity Gaming Services Setup

#### Link Project to Unity Cloud:
1. Open `Edit > Project Settings > Services`
2. Click "Create Unity ID" or sign in
3. Select or create an organization
4. Link your project to a Unity Cloud project

#### Enable Required Services:
1. Go to [Unity Dashboard](https://dashboard.unity.com/)
2. Select your project
3. Enable:
   - ✅ **Multiplayer** (includes Relay & Lobby)
   - ✅ **Vivox** (for voice chat)
   - ✅ **Authentication**

---

## 🎮 Scene Setup

### Step 1: Create Managers

1. **Create Empty GameObject**: Right-click in Hierarchy → `Create Empty`
2. **Name it**: `MultiplayerManagers`
3. **Add Components**:
   - `MultiplayerSessionManager`
   - `MultiplayerMysteryCoordinator`
   - (Keep existing `MysteryAPIService`, `MysteryLoader`)

### Step 2: NetworkManager Setup

1. **Create NetworkManager**:
   - Right-click in Hierarchy → `Netcode > NetworkManager`
   
2. **Configure NetworkManager**:
