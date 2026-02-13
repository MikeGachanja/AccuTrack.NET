# AccuTrackOS Build Instructions
## Buildroot-based Linux OS for Radxa Rock 3B HMI/SCADA Runtime

**Target Board:** Radxa Rock 3B - Rockchip RK3568J  
**Board Specifications:**
- **SoC:** Rockchip RK3568J (Quad-core Cortex-A55 @ 2.0GHz, ARMv8)
- **GPU:** Mali-G52 MP2 (OpenGL ES 3.2, Vulkan 1.1)
- **NPU:** 0.8-1.0 TOPS (INT8/INT16/FP16/BFP16)
- **RAM:** 2GB/4GB/8GB LPDDR4
- **Storage:** eMMC connector, microSD card slot, M.2 NVMe support
- **Networking:** 2x Gigabit Ethernet
- **Display:** HDMI 2.0 (4K@60fps), 2x MIPI DSI, eDP

**Build Environment:** Windows 11 Pro (using WSL2)

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Setting Up Build Environment](#setting-up-build-environment)
3. [Buildroot Configuration](#buildroot-configuration)
4. [Kernel Configuration](#kernel-configuration)
5. [Qt 6.x Integration](#qt-6x-integration)
6. [NPU Driver Integration](#npu-driver-integration)
7. [GPU/Graphics Configuration](#gpugraphics-configuration)
8. [Runtime Application Integration](#runtime-application-integration)
9. [Boot Optimization](#boot-optimization)
10. [Building the Image](#building-the-image)
11. [Flashing to Board](#flashing-to-board)
12. [Testing and Validation](#testing-and-validation)
13. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Hardware Requirements
- Radxa Rock 3B board (RK3568J variant)
- microSD card (minimum 16GB, Class 10 or better) or eMMC module
- USB Type-C cable for power (12V/2A recommended)
- HDMI display for testing
- Ethernet cable for network connectivity
- USB-to-Serial adapter (optional, for debugging)

### Software Requirements
- Windows 11 Pro with WSL2 enabled
- At least 50GB free disk space for build
- Internet connection for downloading sources

---

## Setting Up Build Environment

### Step 1: Enable WSL2 on Windows 11

1. Open PowerShell as Administrator and run:
```powershell
wsl --install
```

2. Restart your computer when prompted.

3. After restart, install Ubuntu 22.04 LTS from Microsoft Store:
```powershell
wsl --install -d Ubuntu-22.04
```

4. Launch Ubuntu 22.04 and create your user account.

### Step 2: Configure WSL2 for Buildroot

1. Inside WSL2 Ubuntu terminal, update the system:
```bash
sudo apt update
sudo apt upgrade -y
```

2. Install required build dependencies:
```bash
sudo apt install -y \
    build-essential \
    git \
    libncurses5-dev \
    bc \
    u-boot-tools \
    python3 \
    python3-dev \
    swig \
    libpython3-dev \
    libssl-dev \
    device-tree-compiler \
    bison \
    flex \
    wget \
    cpio \
    unzip \
    rsync \
    file \
    cmake \
    ninja-build \
    libtool \
    autoconf \
    automake \
    pkg-config
```

3. Configure git (if not already done):
```bash
git config --global user.name "Your Name"
git config --global user.email "your.email@example.com"
```

### Step 3: Create Build Directory Structure

```bash
# Create workspace
mkdir -p ~/accutrack-build
cd ~/accutrack-build

# Create directories for organization
mkdir -p {buildroot,kernel,u-boot,rkbin,runtime,output}
```

---

## Buildroot Configuration

### Step 1: Clone Buildroot

```bash
cd ~/accutrack-build/buildroot
git clone https://git.buildroot.net/buildroot
cd buildroot

# Use latest stable LTS release
git checkout 2024.02.x  # Or latest stable version
```

### Step 2: Clone Rockchip BSP Repository

```bash
cd ~/accutrack-build

# Clone the stable RK35xx kernel (supports RK3566/RK3568)
git clone https://github.com/unifreq/linux-5.10.y-rk35xx.git kernel
cd kernel
# Use the latest stable commit or a specific tag
git checkout main
cd ..

# Clone rkbin (bootloader binaries)
git clone https://github.com/rockchip-linux/rkbin.git rkbin

# Clone mainline U-Boot (has Rock 3B support)
git clone https://github.com/u-boot/u-boot.git u-boot
cd u-boot
# Use a recent stable version that has Rock 3B support (added in 2024)
git checkout v2024.10
cd ..
```

**Note on Repositories:**
- The old `rockchip-linux/kernel` with `stable-5.10-rock3` branch no longer exists
- Using `unifreq/linux-5.10.y-rk35xx` which is the stable, well-maintained BSP kernel for RK3568
- Mainline U-Boot now has official Rock 3B support (defconfig: `rock-3b-rk3568_defconfig`)

**Alternative Kernel Repositories** (if you encounter issues):
1. **Armbian's Rockchip kernel** (also based on 5.10):
   ```bash
   git clone https://github.com/armbian/linux-rockchip.git -b rk-5.10-rkr6 kernel
   ```

2. **FriendlyARM's kernel** (good RK3568 support):
   ```bash
   git clone https://github.com/friendlyarm/kernel-rockchip.git -b nanopi5-v5.10.y_opt kernel
   ```

3. **Mainline Linux** (6.6+ has basic RK3568 support, but limited multimedia):
   ```bash
   git clone https://git.kernel.org/pub/scm/linux/kernel/git/stable/linux.git -b linux-6.6.y kernel
   ```

For this guide, we recommend `unifreq/linux-5.10.y-rk35xx` as it has the best hardware support including GPU (Mali), NPU, VPU, and all peripherals.

### Step 3: Initial Buildroot Configuration

```bash
cd ~/accutrack-build/buildroot/buildroot

# Start with a base ARM64 defconfig
make qemu_aarch64_virt_defconfig

# Now customize for Rock 3B
make menuconfig
```

### Step 4: Buildroot menuconfig Settings

Navigate through the menu and configure the following:

#### **Target Options**
```
Target Architecture: AArch64 (little endian)
Target Architecture Variant: cortex-a55
```

#### **Toolchain**
```
Toolchain type: Buildroot toolchain
Kernel Headers: Linux 5.10.x (match your kernel version)
C library: glibc
GCC compiler version: gcc 11.x or newer
Enable C++ support: YES
Enable WCHAR support: YES
Enable toolchain locale support: YES
Thread library debugging: NO (for optimized boot)
```

#### **System Configuration**
```
System hostname: accutrack-hmi
System banner: "Welcome to AccuTrackOS"
Root password: (set a secure password for development)
/dev management: Dynamic using devtmpfs + eudev
Enable root login with password: YES
Run a getty (login prompt) after boot: YES (on ttyS2 - UART2 for Rock 3B)

Init system: systemd (provides better service management)

/dev management: Dynamic using devtmpfs + eudev

Enable remounting of root filesystem as read-write: YES
```

#### **Kernel**
```
Kernel: Linux
Kernel version: Custom Git repository
    URL: https://github.com/unifreq/linux-5.10.y-rk35xx.git
    Branch/tag: main
    
Kernel configuration: Using a custom config file
    Configuration file path: (we'll create this later)

Build a Device Tree Blob (DTB): YES
    In-tree Device Tree Source file names: rockchip/rk3568-rock-3b
```

#### **Target Packages**

##### **Networking Applications**
```
[*] dropbear (SSH server) - OR -
[*] openssh (alternative SSH)
    [*] client
    [*] server
    
[*] ethtool
[*] iperf3 (for network testing - if available)
[*] tcpdump (if available)
[*] libmodbus (IMPORTANT: for Modbus TCP/RTU support)
    [*] Install tools (if available)

Note: libmodbus provides Modbus protocol implementation for both
Modbus TCP (over Ethernet) and Modbus RTU (over serial).
Essential for SCADA industrial communication.
```

##### **Hardware Handling**
```
[*] Firmware
    [*] rockchip-firmware (if available)
    [*] linux-firmware
        [*] WiFi firmware (if using WiFi modules)
```

##### **Libraries - Graphics**
```
[*] libdrm
    [*] Install test programs (if available)
[*] wayland (if available)

Note: mesa3d and weston may not be available in your Buildroot version.
If they're not present, we'll rely on the proprietary Mali GPU libraries
which will be added via the custom rockchip-mali package.

The Mali proprietary drivers provide OpenGL ES and EGL support directly.
```

##### **Libraries - Multimedia**
```
[*] libmpeg2 (available for video decoding if needed)

Note: ffmpeg may not be available in your Buildroot version.
libmpeg2 provides MPEG-1 and MPEG-2 video decoding support.
If video playback is needed in your SCADA HMI, this should suffice.
```

##### **Libraries - Networking**
```
[*] libcurl (if available)
[*] open62541 (CRITICAL - OPC UA stack)
    [*] Enable encryption support (if available)
    [*] Build shared library
    [*] Enable discovery
    
Note: open62541 is essential for your SCADA OPC UA server/client functionality.
This is an open-source OPC UA implementation (IEC 62541) that provides
both client and server capabilities.

OpenSSL and mbedtls may not be available in your Buildroot version.
The open62541 library can work with or without encryption depending on
your security requirements. If encryption is needed, you may need to
add OpenSSL/mbedTLS as a custom package or use a newer Buildroot version.
```

##### **Development Tools** (for SSH debugging - if available)
```
Note: gdb, strace, lsof, and htop may not be available in your Buildroot version.

If available, enable:
[ ] gdb (GNU Debugger)
    [ ] gdbserver
[ ] strace
[ ] lsof  
[ ] htop

Alternatives for debugging:
- Use basic 'ps', 'top', 'cat /proc/<pid>/maps' for process monitoring
- Enable kernel debugging options if needed
- Use journalctl for systemd log analysis
- netstat for network debugging
```

##### **System Tools**
```
[*] systemd (already selected as init system)

Note: systemd utilities (networkd, resolved, timesyncd) configuration options
may not be available as separate selections in your Buildroot version.
These are typically compiled into systemd by default.

To verify systemd components after build:
- systemctl list-unit-files | grep systemd-networkd
- systemctl list-unit-files | grep systemd-resolved
- systemctl list-unit-files | grep systemd-timesyncd

If these services are not present, you can use:
- ifupdown + /etc/network/interfaces for networking
- /etc/resolv.conf for DNS
- NTP client packages for time synchronization
```

#### **Filesystem Images**
```
[*] ext2/3/4 root filesystem
    ext2/3/4 variant: ext4
    exact size: leave empty (auto-calculate)
    
[*] tar the root filesystem (for backup/analysis)
```

#### **Bootloaders**
```
[*] U-Boot
    Build system: Kconfig
    Board defconfig: rock-3b-rk3568
    U-Boot Version: Custom Git repository
        Repository URL: https://github.com/u-boot/u-boot.git
        Branch: v2024.10 (or later stable version)
    
    [*] Install U-Boot SPL binary image
    [*] Environment image
    
    U-Boot needs dtc: YES
    U-Boot needs OpenSSL: YES
    U-Boot needs pylibfdt: YES
    
    Note: ATF bl31 options are not available in menuconfig for your
    Buildroot version. We'll handle this manually in the build process.
```

**Important:** You'll need ARM Trusted Firmware (ATF) BL31 binary:
- Get from rkbin: `rkbin/bin/rk35/rk3568_bl31_v1.34.elf`
- We'll configure this via environment variables during build

The ATF BL31 requirement will be handled by:
1. Setting BL31 environment variable before U-Boot build
2. Using a pre-build hook script (see Step 5 in Building the Image section)

Save and exit menuconfig.

---

## Kernel Configuration

### Step 1: Generate Base Kernel Config

```bash
cd ~/accutrack-build/kernel

# Use Rockchip's default config as base
make ARCH=arm64 CROSS_COMPILE=aarch64-linux-gnu- rockchip_linux_defconfig

# Customize kernel
make ARCH=arm64 CROSS_COMPILE=aarch64-linux-gnu- menuconfig
```

### Step 2: Enable Essential Kernel Features

#### **Device Drivers → Graphics Support**
```
[*] Direct Rendering Manager (XFree86 4.1.0 and higher DRI support)
    [*] ARM Mali Midgard/Bifrost GPU support
        [*] Mali-G52 support
    [*] Rockchip DRM Support
        [*] Rockchip specific extensions
    [*] DRM Support for Rockchip VOP
        [*] Rockchip VOP2 driver
```

#### **Device Drivers → NPU Support**
```
[*] RKNPU (Rockchip NPU) driver support
    (This might be in staging drivers or as external module)
```

#### **Device Drivers → Network Device Support**
```
[*] Ethernet driver support
    [*] Rockchip Ethernet driver
        [*] Rockchip GMAC ethernet driver
```

#### **Networking Support**
```
[*] Networking support
    Networking options
        [*] TCP/IP networking
        [*] IP: advanced router
        [*] Unix domain sockets
```

#### **Device Drivers → Industrial I/O Support** (for potential sensor integration)
```
[*] Enable IIO consumer support
```

#### **Device Drivers → Multifunction Device Drivers**
```
[*] Rockchip RK8XX Power Management chip
```

#### **File Systems**
```
[*] Second extended fs support (ext2)
[*] Ext3 journalling file system support
[*] The Extended 4 (ext4) filesystem
[*] Network File Systems
    [*] NFS client support
        [*] NFS client support for NFS version 3
```

#### **Device Drivers → Real Time Clock**
```
[*] RTC support
    [*] Rockchip RTC driver
```

### Step 3: Save Kernel Config

```bash
# Save the configuration
make ARCH=arm64 CROSS_COMPILE=aarch64-linux-gnu- savedefconfig

# Copy to a safe location
cp defconfig ~/accutrack-build/kernel/defconfig_rock3b_accutrack

# Copy device tree to verify
cp arch/arm64/boot/dts/rockchip/rk3568-rock-3b.dts ~/accutrack-build/kernel/
```

### Step 4: Update Buildroot to Use Custom Kernel Config

```bash
cd ~/accutrack-build/buildroot/buildroot
make menuconfig
```

Navigate to:
```
Kernel → Kernel configuration
    [*] Using a custom (def)config file
    Configuration file path: /home/[username]/accutrack-build/kernel/defconfig_rock3b_accutrack
```

---

## Qt 6.x Integration

### Step 1: Enable Qt6 in Buildroot

```bash
cd ~/accutrack-build/buildroot/buildroot
make menuconfig
```

#### Navigate to: Target packages → Graphic libraries and applications (graphic/text) → Qt6

```
[*] qt6base
    [*] gui module
    [*] widgets module
    [*] opengl support
        OpenGL API: OpenGL ES 2.0+
    [*] linuxfb support
    [*] eglfs support
        [*] eglfs kms support
    [*] fontconfig support
    [*] PNG support
    [*] JPEG support
    [*] harfbuzz support
    [*] enable sql module
        [*] SQLite support
    [*] enable network module

[*] qt6declarative (QML)
    [*] install qml tools

[*] qt6multimedia (if video/audio needed)
    [*] ffmpeg support

[*] qt6serialport (for Modbus RTU if needed)

[*] qt6svg

[*] qt6imageformats (extended image format support)

[*] qt6tools (for Qt Designer if needed during development)
```

### Step 2: Configure Qt Platform Plugin

Since this is a direct-to-Runtime HMI system, we need to configure Qt to use the optimal graphics backend.

Create a Qt environment configuration:

```bash
mkdir -p ~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay/etc/profile.d/
```

Create file: `~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay/etc/profile.d/qt6-env.sh`

```bash
#!/bin/sh
# Qt 6 Environment Configuration for AccuTrackOS

# Use EGLFS with KMS backend for best performance with Mali GPU
export QT_QPA_PLATFORM=eglfs
export QT_QPA_EGLFS_INTEGRATION=eglfs_kms

# Enable GPU acceleration
export QT_QPA_EGLFS_KMS_CONFIG=/etc/qt-kms-config.json

# Disable Qt debug output in production
export QT_LOGGING_RULES="*.debug=false"

# Set default font DPI if needed
export QT_FONT_DPI=96

# Enable threaded OpenGL (if stable with Mali driver)
export QT_OPENGL_THREADED=1
```

Create KMS configuration: `~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay/etc/qt-kms-config.json`

```json
{
  "device": "/dev/dri/card0",
  "outputs": [
    {
      "name": "HDMI1",
      "mode": "1920x1080@60",
      "format": "argb8888",
      "physicalWidth": 480,
      "physicalHeight": 270
    }
  ]
}
```

---

## NPU Driver Integration

The Rockchip RK3568J NPU (0.8-1.0 TOPS) requires the RKNPU driver and runtime libraries.

### Step 1: Download Rockchip NPU SDK

```bash
cd ~/accutrack-build
git clone https://github.com/rockchip-linux/rknpu2.git
```

### Step 2: Create Buildroot Package for RKNPU

Create directory structure:

```bash
mkdir -p ~/accutrack-build/buildroot/buildroot/package/rknpu2
```

Create `~/accutrack-build/buildroot/buildroot/package/rknpu2/Config.in`:

```makefile
config BR2_PACKAGE_RKNPU2
    bool "rknpu2"
    depends on BR2_aarch64
    help
      Rockchip NPU (Neural Processing Unit) runtime library and drivers
      for RK356x series SoCs with 0.8-1.0 TOPS AI acceleration.
      
      Supports frameworks: TensorFlow Lite, PyTorch, ONNX, Caffe
      
      https://github.com/rockchip-linux/rknpu2
```

Create `~/accutrack-build/buildroot/buildroot/package/rknpu2/rknpu2.mk`:

```makefile
################################################################################
#
# rknpu2
#
################################################################################

RKNPU2_VERSION = main
RKNPU2_SITE = https://github.com/rockchip-linux/rknpu2.git
RKNPU2_SITE_METHOD = git
RKNPU2_INSTALL_STAGING = YES
RKNPU2_INSTALL_TARGET = YES

define RKNPU2_INSTALL_TARGET_CMDS
    $(INSTALL) -D -m 0755 $(@D)/runtime/RK356X/Linux/librknn_api/aarch64/librknnrt.so \
        $(TARGET_DIR)/usr/lib/librknnrt.so
    
    $(INSTALL) -D -m 0644 $(@D)/runtime/RK356X/Linux/librknn_api/include/rknn_api.h \
        $(TARGET_DIR)/usr/include/rknn_api.h
    
    $(INSTALL) -D -m 0644 $(@D)/runtime/RK356X/Linux/librknn_api/include/rknn_custom_op.h \
        $(TARGET_DIR)/usr/include/rknn_custom_op.h
    
    # Install NPU driver if needed
    if [ -f $(@D)/driver/npu_ko/galcore.ko ]; then \
        $(INSTALL) -D -m 0644 $(@D)/driver/npu_ko/galcore.ko \
            $(TARGET_DIR)/lib/modules/galcore.ko; \
    fi
endef

define RKNPU2_INSTALL_STAGING_CMDS
    $(INSTALL) -D -m 0755 $(@D)/runtime/RK356X/Linux/librknn_api/aarch64/librknnrt.so \
        $(STAGING_DIR)/usr/lib/librknnrt.so
    
    $(INSTALL) -D -m 0644 $(@D)/runtime/RK356X/Linux/librknn_api/include/rknn_api.h \
        $(STAGING_DIR)/usr/include/rknn_api.h
    
    $(INSTALL) -D -m 0644 $(@D)/runtime/RK356X/Linux/librknn_api/include/rknn_custom_op.h \
        $(STAGING_DIR)/usr/include/rknn_custom_op.h
endef

$(eval $(generic-package))
```

### Step 3: Register Package in Buildroot

Edit `~/accutrack-build/buildroot/buildroot/package/Config.in`:

Add under appropriate section (search for "Hardware handling"):
```makefile
    source "package/rknpu2/Config.in"
```

### Step 4: Enable RKNPU in Buildroot

```bash
cd ~/accutrack-build/buildroot/buildroot
make menuconfig
```

Navigate to:
```
Target packages → Hardware handling
    [*] rknpu2
```

### Step 5: NPU Kernel Module Loading

Create systemd service for NPU initialization:

File: `~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay/etc/systemd/system/rknpu-init.service`

```ini
[Unit]
Description=Rockchip NPU Initialization
Before=runtime.service

[Service]
Type=oneshot
ExecStart=/usr/bin/modprobe galcore
RemainAfterExit=yes

[Install]
WantedBy=multi-user.target
```

---

## GPU/Graphics Configuration

### Step 1: Mali GPU Userspace Drivers

The Mali-G52 GPU requires proprietary userspace blobs from ARM/Rockchip.

```bash
cd ~/accutrack-build/buildroot/buildroot
make menuconfig
```

Navigate to:
```
Target packages → Hardware handling → Firmware
    [*] rockchip-mali (if available in your buildroot version)
```

If not available as a package, you'll need to add it manually:

### Step 2: Create Mali GPU Package

```bash
mkdir -p ~/accutrack-build/buildroot/buildroot/package/rockchip-mali
```

Create `~/accutrack-build/buildroot/buildroot/package/rockchip-mali/Config.in`:

```makefile
config BR2_PACKAGE_ROCKCHIP_MALI
    bool "rockchip-mali"
    depends on BR2_aarch64
    select BR2_PACKAGE_HAS_LIBEGL
    select BR2_PACKAGE_HAS_LIBGLES
    help
      ARM Mali GPU userspace driver for Rockchip RK356x series
      
      Mali-G52 GPU support with OpenGL ES 3.2 and Vulkan 1.1
```

Create `~/accutrack-build/buildroot/buildroot/package/rockchip-mali/rockchip-mali.mk`:

```makefile
################################################################################
#
# rockchip-mali
#
################################################################################

ROCKCHIP_MALI_VERSION = main
ROCKCHIP_MALI_SITE = https://github.com/rockchip-linux/libmali.git
ROCKCHIP_MALI_SITE_METHOD = git
ROCKCHIP_MALI_INSTALL_STAGING = YES

define ROCKCHIP_MALI_INSTALL_TARGET_CMDS
    $(INSTALL) -D -m 0755 $(@D)/lib/aarch64-linux-gnu/libmali-bifrost-g52-g2p0-wayland-gbm.so \
        $(TARGET_DIR)/usr/lib/libmali.so
    
    cd $(TARGET_DIR)/usr/lib && \
        ln -sf libmali.so libEGL.so.1 && \
        ln -sf libmali.so libEGL.so && \
        ln -sf libmali.so libGLESv1_CM.so.1 && \
        ln -sf libmali.so libGLESv1_CM.so && \
        ln -sf libmali.so libGLESv2.so.2 && \
        ln -sf libmali.so libGLESv2.so && \
        ln -sf libmali.so libgbm.so.1 && \
        ln -sf libmali.so libgbm.so
endef

define ROCKCHIP_MALI_INSTALL_STAGING_CMDS
    $(INSTALL) -D -m 0755 $(@D)/lib/aarch64-linux-gnu/libmali-bifrost-g52-g2p0-wayland-gbm.so \
        $(STAGING_DIR)/usr/lib/libmali.so
    
    cd $(STAGING_DIR)/usr/lib && \
        ln -sf libmali.so libEGL.so.1 && \
        ln -sf libmali.so libEGL.so && \
        ln -sf libmali.so libGLESv1_CM.so.1 && \
        ln -sf libmali.so libGLESv1_CM.so && \
        ln -sf libmali.so libGLESv2.so.2 && \
        ln -sf libmali.so libGLESv2.so && \
        ln -sf libmali.so libgbm.so.1 && \
        ln -sf libmali.so libgbm.so
endef

$(eval $(generic-package))
```

### Step 3: Register Mali Package

Edit `~/accutrack-build/buildroot/buildroot/package/Config.in`:

```makefile
    source "package/rockchip-mali/Config.in"
```

Enable in menuconfig:
```
Target packages → Hardware handling
    [*] rockchip-mali
```

---

## OPC UA Integration (open62541)

Your AccuTrackOS includes the open62541 library, which provides OPC UA (IEC 62541) server and client capabilities for industrial SCADA communication.

### Step 1: Verify open62541 Installation

After building, the following should be available on the target system:

```bash
# Check library
ls -l /usr/lib/libopen62541.so*

# Check headers (if development files installed)
ls -l /usr/include/open62541/
```

### Step 2: Runtime Integration with open62541

Your Qt Runtime application will need to link against open62541. Ensure your build system includes:

**CMakeLists.txt** (if using CMake):
```cmake
find_package(open62541 REQUIRED)
target_link_libraries(Runtime open62541::open62541)
```

**QMake .pro file** (if using qmake):
```qmake
LIBS += -lopen62541
INCLUDEPATH += /usr/include/open62541
```

### Step 3: OPC UA Server Configuration

The Runtime should initialize an OPC UA server for SCADA operations:

**Example Configuration** (to be implemented in Runtime):
```cpp
// Basic OPC UA server setup
UA_Server *server = UA_Server_new();
UA_ServerConfig *config = UA_Server_getConfig(server);

// Configure network endpoint
UA_ServerConfig_setMinimal(config, 4840, NULL);

// Set server description
config->applicationDescription.applicationName = 
    UA_LOCALIZEDTEXT("en-US", "AccuTrack SCADA Runtime");

// Run server in separate thread or event loop
```

### Step 4: Network Configuration for OPC UA

Ensure proper network setup for OPC UA communication:

**Default OPC UA Port:** 4840 (TCP)

Update firewall rules if needed (file: `rootfs-overlay/etc/systemd/system/opcua-firewall.service`):

```ini
[Unit]
Description=Configure firewall for OPC UA
Before=runtime.service

[Service]
Type=oneshot
ExecStart=/usr/sbin/iptables -A INPUT -p tcp --dport 4840 -j ACCEPT
ExecStart=/usr/sbin/iptables -A INPUT -p tcp --dport 502 -j ACCEPT
RemainAfterExit=yes

[Install]
WantedBy=multi-user.target
```

### Step 5: OPC UA Discovery

For OPC UA discovery mechanisms (LDS), you may need additional configuration:

```bash
# Enable multicast for OPC UA discovery
# Add to network configuration
```

### Step 6: Modbus TCP Integration

Since you mentioned Modbus as well, you'll need additional libraries:

**Option 1:** libmodbus
```bash
# In buildroot menuconfig:
Target packages → Networking applications → libmodbus
```

**Option 2:** Integrate Modbus directly in Runtime application

Your Runtime can act as:
- **OPC UA Server/Client** (using open62541)
- **Modbus TCP Master/Slave** (using libmodbus or custom implementation)

This allows bridging between Modbus devices and OPC UA clients.

### Step 7: Security Considerations

**For production deployments:**

1. **Enable OPC UA Security:**
   - Certificate-based authentication
   - Message encryption
   - User authentication

2. **Network Security:**
   - Use VLANs for industrial network isolation
   - Configure firewall rules
   - Disable unused services

3. **Update open62541 Configuration:**
```cpp
// Enable security in open62541
UA_ServerConfig_setDefaultWithSecurityPolicies(
    config, 
    4840,
    certificate, 
    privateKey,
    trustedCertificates,
    trustedCertificatesSize,
    issuerLists,
    issuerListsSize,
    revocationLists,
    revocationListsSize
);
```

**Note:** Full security setup requires OpenSSL or mbedTLS, which may require adding as custom packages to your Buildroot build.

---

## Runtime Application Integration

### Step 1: Prepare Runtime Application

Ensure your Qt Runtime application is cross-compiled for ARM64. If not already done:

```bash
# On your development machine with Qt 6.x for ARM64:
# (This would typically be done outside WSL with proper Qt cross-compile setup)

# For now, we'll assume you have a pre-built ARM64 Runtime binary
```

### Step 2: Create Runtime Package for Buildroot

```bash
mkdir -p ~/accutrack-build/buildroot/buildroot/package/accutrack-runtime
```

Create `~/accutrack-build/buildroot/buildroot/package/accutrack-runtime/Config.in`:

```makefile
config BR2_PACKAGE_ACCUTRACK_RUNTIME
    bool "accutrack-runtime"
    depends on BR2_PACKAGE_QT6BASE
    help
      AccuTrack SCADA Runtime application
      
      Qt 6.x based HMI/SCADA runtime environment for industrial automation.
```

Create `~/accutrack-build/buildroot/buildroot/package/accutrack-runtime/accutrack-runtime.mk`:

```makefile
################################################################################
#
# accutrack-runtime
#
################################################################################

ACCUTRACK_RUNTIME_VERSION = 1.0.0
# Option 1: If you have Runtime in a git repository
# ACCUTRACK_RUNTIME_SITE = https://your-git-repo.com/runtime.git
# ACCUTRACK_RUNTIME_SITE_METHOD = git

# Option 2: If you have a local tarball
ACCUTRACK_RUNTIME_SITE = $(TOPDIR)/../runtime
ACCUTRACK_RUNTIME_SITE_METHOD = local

ACCUTRACK_RUNTIME_DEPENDENCIES = qt6base qt6declarative

define ACCUTRACK_RUNTIME_INSTALL_TARGET_CMDS
    # Install Runtime binary
    $(INSTALL) -D -m 0755 $(@D)/Runtime $(TARGET_DIR)/usr/bin/Runtime
    
    # Install any required libraries
    if [ -d $(@D)/lib ]; then \
        cp -r $(@D)/lib/* $(TARGET_DIR)/usr/lib/; \
    fi
    
    # Install QML modules if any
    if [ -d $(@D)/qml ]; then \
        mkdir -p $(TARGET_DIR)/usr/lib/qt6/qml; \
        cp -r $(@D)/qml/* $(TARGET_DIR)/usr/lib/qt6/qml/; \
    fi
    
    # Install resources/assets
    if [ -d $(@D)/resources ]; then \
        mkdir -p $(TARGET_DIR)/opt/accutrack/resources; \
        cp -r $(@D)/resources/* $(TARGET_DIR)/opt/accutrack/resources/; \
    fi
    
    # Install default project (generic start page)
    if [ -d $(@D)/default-project ]; then \
        mkdir -p $(TARGET_DIR)/opt/accutrack/projects; \
        cp -r $(@D)/default-project $(TARGET_DIR)/opt/accutrack/projects/default; \
    fi
endef

$(eval $(generic-package))
```

### Step 3: Prepare Runtime Files

Create the runtime directory structure:

```bash
mkdir -p ~/accutrack-build/runtime
cd ~/accutrack-build/runtime

# Create subdirectories
mkdir -p {lib,qml,resources,default-project}

# Copy your compiled Runtime binary here
# cp /path/to/your/Runtime ./Runtime

# Copy any dependent libraries
# Copy QML modules
# Copy resources
# Copy default project files
```

### Step 4: Create systemd Service for Runtime

File: `~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay/etc/systemd/system/runtime.service`

```ini
[Unit]
Description=AccuTrack SCADA Runtime
After=multi-user.target systemd-user-sessions.service rknpu-init.service
Requires=rknpu-init.service

[Service]
Type=simple
User=root
Environment="QT_QPA_PLATFORM=eglfs"
Environment="QT_QPA_EGLFS_INTEGRATION=eglfs_kms"
Environment="QT_QPA_EGLFS_KMS_CONFIG=/etc/qt-kms-config.json"
Environment="QT_LOGGING_RULES=*.debug=false"
Environment="XDG_RUNTIME_DIR=/run/user/0"
WorkingDirectory=/opt/accutrack
ExecStart=/usr/bin/Runtime
Restart=always
RestartSec=5
StandardOutput=journal
StandardError=journal

# CPU and memory limits (optional, adjust as needed)
CPUQuota=400%
MemoryLimit=1G

[Install]
WantedBy=graphical.target
```

### Step 5: Register Runtime Package

Edit `~/accutrack-build/buildroot/buildroot/package/Config.in`:

Under "Graphic applications":
```makefile
    source "package/accutrack-runtime/Config.in"
```

Enable in menuconfig:
```
Target packages → Graphic libraries and applications (graphic/text)
    [*] accutrack-runtime
```

---

## Boot Optimization

To achieve optimal (fastest) boot time:

### Step 1: Kernel Optimization

Edit kernel config:
```bash
cd ~/accutrack-build/kernel
make ARCH=arm64 CROSS_COMPILE=aarch64-linux-gnu- menuconfig
```

Disable:
```
General setup
    [ ] Initial RAM filesystem and RAM disk (initramfs/initrd) support (if not needed)
    
Kernel Features
    [ ] Support for hot-pluggable CPUs (if not needed)
    [ ] CPU idle PM support (disable for faster boot, enable for power saving)

Device Drivers → Generic Driver Options
    [ ] Support for uevent helper (speeds up boot)
```

Enable:
```
Processor type and features
    [*] Symmetric multi-processing support
    [*] Enable the LRU to spot cache page references
```

### Step 2: U-Boot Optimization

Create U-Boot environment file:

File: `~/accutrack-build/buildroot/buildroot/board/accutrack/uboot-env.txt`

```bash
# Fast boot configuration
bootdelay=0
silent=1

# Boot command
bootcmd=run distro_bootcmd

# Kernel command line optimizations
bootargs=console=ttyS2,1500000 root=/dev/mmcblk0p2 rootwait rw quiet loglevel=3 fsck.mode=skip
```

### Step 3: Systemd Boot Optimization

File: `~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay/etc/systemd/system.conf.d/boot-optimization.conf`

```ini
[Manager]
# Reduce default timeout
DefaultTimeoutStartSec=15s
DefaultTimeoutStopSec=10s

# Disable unnecessary services startup timeout
DefaultDeviceTimeoutSec=10s
```

### Step 4: Disable Unnecessary Services

Create file: `~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay/etc/systemd/system-preset/90-accutrack.preset`

```ini
# Disable unnecessary services for faster boot
disable systemd-networkd-wait-online.service
disable systemd-timesyncd.service  # Enable after boot if time sync needed
disable debug-shell.service

# Enable essential services
enable rknpu-init.service
enable runtime.service
```

### Step 5: Kernel Boot Parameters

Modify: `~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay/boot/extlinux/extlinux.conf`

```
label AccuTrackOS
    kernel /boot/Image
    fdt /boot/rk3568-rock-3b.dtb
    append console=ttyS2,1500000 root=/dev/mmcblk0p2 rootwait rw quiet loglevel=3 logo.nologo vt.global_cursor_default=0
```

Parameters explanation:
- `quiet`: Suppress most boot messages
- `loglevel=3`: Only show errors
- `logo.nologo`: Disable boot logo (slightly faster)
- `vt.global_cursor_default=0`: Hide cursor during boot

---

## Building the Image

### Step 1: Final Buildroot Configuration

```bash
cd ~/accutrack-build/buildroot/buildroot

# Run menuconfig one last time to verify all settings
make menuconfig
```

Key final checks:
```
System configuration
    Root filesystem overlay directories: board/accutrack/rootfs-overlay
    
    Custom scripts to run before creating filesystem images:
        board/accutrack/post-build.sh (if you create one)
    
    Custom scripts to run after creating filesystem images:
        board/accutrack/post-image.sh (create this for image preparation)

Filesystem images
    [*] ext2/3/4 root filesystem
        ext4 variant
    [*] tar the root filesystem
```

### Step 2: Create Post-Build Script

File: `~/accutrack-build/buildroot/buildroot/board/accutrack/post-build.sh`

```bash
#!/bin/bash

set -e

TARGET_DIR=$1

echo "AccuTrackOS Post-Build Script"

# Enable systemd services
ln -sf /etc/systemd/system/runtime.service \
    ${TARGET_DIR}/etc/systemd/system/graphical.target.wants/runtime.service

ln -sf /etc/systemd/system/rknpu-init.service \
    ${TARGET_DIR}/etc/systemd/system/multi-user.target.wants/rknpu-init.service

# Set hostname
echo "accutrack-hmi" > ${TARGET_DIR}/etc/hostname

# Configure network (static IP example for industrial use)
mkdir -p ${TARGET_DIR}/etc/systemd/network
cat > ${TARGET_DIR}/etc/systemd/network/10-eth0.network << 'EOF'
[Match]
Name=eth0

[Network]
DHCP=yes

# Or for static IP:
# Address=192.168.1.100/24
# Gateway=192.168.1.1
# DNS=8.8.8.8
EOF

# Enable networkd
ln -sf /usr/lib/systemd/system/systemd-networkd.service \
    ${TARGET_DIR}/etc/systemd/system/multi-user.target.wants/systemd-networkd.service

# Set permissions
chmod 755 ${TARGET_DIR}/etc/profile.d/qt6-env.sh

echo "Post-build completed successfully"
```

Make executable:
```bash
chmod +x ~/accutrack-build/buildroot/buildroot/board/accutrack/post-build.sh
```

### Step 3: Create Post-Image Script

File: `~/accutrack-build/buildroot/buildroot/board/accutrack/post-image.sh`

```bash
#!/bin/bash

set -e

BOARD_DIR="$(dirname $0)"
GENIMAGE_CFG="${BOARD_DIR}/genimage-rock3b.cfg"
GENIMAGE_TMP="${BUILD_DIR}/genimage.tmp"

# Create genimage config if it doesn't exist
if [ ! -f "${GENIMAGE_CFG}" ]; then
    cat > "${GENIMAGE_CFG}" << 'EOF'
image boot.img {
    hdimage {
        partition-table-type = "gpt"
    }
    
    partition loader1 {
        image = "idbloader.img"
        offset = 32K
    }
    
    partition loader2 {
        image = "u-boot.itb"
        offset = 8M
    }
    
    partition rootfs {
        partition-type = 0x83
        image = "rootfs.ext4"
        size = 2G
    }
}
EOF
fi

# Generate final image
rm -rf "${GENIMAGE_TMP}"

genimage \
    --rootpath "${TARGET_DIR}" \
    --tmppath "${GENIMAGE_TMP}" \
    --inputpath "${BINARIES_DIR}" \
    --outputpath "${BINARIES_DIR}" \
    --config "${GENIMAGE_CFG}"

echo "AccuTrackOS image created: ${BINARIES_DIR}/boot.img"
```

Make executable:
```bash
chmod +x ~/accutrack-build/buildroot/buildroot/board/accutrack/post-image.sh
```

### Step 4: Create Build Directory Structure

```bash
mkdir -p ~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay
mkdir -p ~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay/etc/systemd/system
mkdir -p ~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay/etc/profile.d
mkdir -p ~/accutrack-build/buildroot/buildroot/board/accutrack/rootfs-overlay/opt/accutrack
```

### Step 5: Build AccuTrackOS

```bash
cd ~/accutrack-build/buildroot/buildroot

# Before building, we need to set up ATF BL31 for U-Boot
# Create a pre-build hook for U-Boot

mkdir -p board/accutrack
cat > board/accutrack/uboot-pre-build.sh << 'EOF'
#!/bin/bash
# Copy BL31 and TPL binaries for U-Boot build

RKBIN_DIR="${TOPDIR}/../rkbin"
UBOOT_BUILD_DIR="${BUILD_DIR}/uboot-custom"

if [ -d "${UBOOT_BUILD_DIR}" ]; then
    # Copy BL31 (ARM Trusted Firmware)
    export BL31="${RKBIN_DIR}/bin/rk35/rk3568_bl31_v1.34.elf"
    
    # Copy TPL (DDR init)
    export ROCKCHIP_TPL="${RKBIN_DIR}/bin/rk35/rk3568_ddr_1560MHz_v1.13.bin"
    
    echo "U-Boot pre-build: BL31=${BL31}"
    echo "U-Boot pre-build: TPL=${ROCKCHIP_TPL}"
fi
EOF

chmod +x board/accutrack/uboot-pre-build.sh

# Update buildroot config to use this hook
# In menuconfig: Bootloaders -> U-Boot -> U-Boot needs hooks
# Or add to defconfig:
# BR2_TARGET_UBOOT_NEEDS_HOOKS=y
# BR2_TARGET_UBOOT_HOOKS_DIR="board/accutrack"

# Clean previous builds (if any)
make clean

# Start the build (this will take 1-3 hours depending on your system)
make all -j$(nproc)
```

**Important Notes on U-Boot Build:**
- The RK3568 U-Boot requires ATF BL31 (ARM Trusted Firmware) and TPL (DDR initialization)
- These binaries are provided in the `rkbin` repository
- Buildroot will automatically use them if properly configured
- If the build fails with "BL31 not found", verify the rkbin paths

**Alternative Manual U-Boot Build** (if buildroot integration fails):
```bash
cd ~/accutrack-build/u-boot

# Set environment variables for build
export BL31=../rkbin/bin/rk35/rk3568_bl31_v1.34.elf
export ROCKCHIP_TPL=../rkbin/bin/rk35/rk3568_ddr_1560MHz_v1.13.bin
export CROSS_COMPILE=aarch64-linux-gnu-

# Configure and build
make rock-3b-rk3568_defconfig
make -j$(nproc)

# Output files will be:
# - idbloader.img (TPL + SPL)
# - u-boot.itb (U-Boot proper with FIT image)
# Copy these to buildroot output/images manually if needed
```

Monitor the build:
```bash
# The build process will:
# 1. Download all source packages
# 2. Build cross-compilation toolchain
# 3. Build Linux kernel
# 4. Build U-Boot
# 5. Build all target packages (Qt, libraries, etc.)
# 6. Build Runtime application
# 7. Create root filesystem
# 8. Generate bootable image
```

### Step 6: Build Output

After successful build, outputs will be in:
```
~/accutrack-build/buildroot/buildroot/output/images/
```

Key files:
- `Image` - Linux kernel
- `rk3568-rock-3b.dtb` - Device tree blob
- `rootfs.ext4` - Root filesystem
- `u-boot.itb` - U-Boot image
- `idbloader.img` - Boot loader
- `boot.img` - Complete bootable image (if genimage succeeded)

---

## Flashing to Board

### Method 1: Flash to microSD Card (Recommended for First Test)

#### On Windows (using Rufus or Win32DiskImager):

1. Install Rufus from: https://rufus.ie/

2. Insert microSD card into Windows PC

3. Open Rufus:
   - Device: Select your microSD card
   - Boot selection: Click "SELECT" and choose `boot.img` or `rootfs.ext4`
   - Partition scheme: GPT
   - Target system: UEFI (non CSM)
   - Click "START"

#### On WSL2/Linux:

```bash
# DANGER: Be absolutely sure you have the correct device!
# This will ERASE ALL DATA on the target device!

# First, identify your SD card device
# Remove SD card, run:
ls /dev/sd*

# Insert SD card, run again:
ls /dev/sd*
# The new device that appears is your SD card (e.g., /dev/sdb)

# Unmount all partitions
sudo umount /dev/sdX*

# Write image to SD card
cd ~/accutrack-build/buildroot/buildroot/output/images/
sudo dd if=boot.img of=/dev/sdX bs=4M status=progress conv=fsync

# Or if you only have rootfs.ext4, you need to manually create partitions:
# (More complex, use rkdeveloptool or upgrade_tool instead - see Method 2)
```

### Method 2: Flash to eMMC (Using Rockchip Tools)

#### Prerequisites:
1. Install Rockchip flash tools on Windows

2. Download from Radxa Wiki or Rockchip:
   - RKDevTool (Windows)
   - Or rkdeveloptool (Linux)

#### Steps for Windows (RKDevTool):

1. Put Rock 3B into Maskrom Mode:
   - Power off board
   - Press and hold MASKROM button (or short MASKROM pins)
   - Connect USB-C cable to PC
   - Power on board
   - Release MASKROM button after 2 seconds

2. Open RKDevTool:
   - Device should show "Found One MASKROM Device"

3. Load firmware:
   - Download Loader: Click "..." and select `rk3568_ddr_1560MHz_v1.xx.bin` from rkbin folder
   - Click "Run"

4. Write images to partitions:
   - Go to "Upgrade Firmware" tab
   - Select `boot.img` or individual components
   - Click "Upgrade"

5. After flashing:
   - Disconnect USB
   - Insert SD card or ensure eMMC is properly flashed
   - Power cycle board

### Method 3: Network Boot (Advanced)

For development, you can set up TFTP/NFS boot:

1. Configure U-Boot for network boot
2. Set up TFTP server with kernel and DTB
3. Set up NFS server with rootfs
4. Configure U-Boot environment variables

(Details omitted for brevity - refer to U-Boot documentation)

---

## Testing and Validation

### Step 1: Initial Boot

1. Insert microSD card (or ensure eMMC is flashed)
2. Connect HDMI display
3. Connect Ethernet cable
4. Connect USB-C power (12V/2A)
5. Power on

Expected boot sequence:
```
U-Boot SPL 2021.xx (Build info)
U-Boot 2021.xx (Build info)
Loading kernel...
Starting kernel...

[  0.000000] Booting Linux on physical CPU 0x0
...
[  OK  ] Started rknpu-init.service
[  OK  ] Started runtime.service
...

AccuTrack Runtime should appear on HDMI display
```

### Step 2: Check System Status

Access via SSH (if enabled):
```bash
ssh root@<board-ip-address>
# Use password you set in buildroot config
```

Verify services:
```bash
# Check Runtime is running
systemctl status runtime.service

# Check NPU driver loaded
lsmod | grep galcore

# Check GPU
ls -l /dev/dri/card0

# Check network
ip addr show
ping 8.8.8.8

# Check disk usage
df -h

# Check Qt environment
echo $QT_QPA_PLATFORM
```

### Step 3: Test GPU Acceleration

```bash
# Check OpenGL ES
eglinfo

# Check if Mali driver loaded
dmesg | grep -i mali

# Test Qt EGLFS
export QT_QPA_PLATFORM=eglfs
export QT_LOGGING_RULES="qt.qpa.*=true"
# Run a simple Qt application to see backend logs
```

### Step 4: Test NPU

```bash
# Check NPU device
ls -l /dev/rknpu

# Check RKNN runtime
ldconfig -p | grep rknn

# Run RKNN test program (if available)
# cd /usr/share/npu/examples
# ./rknn_test
```

### Step 5: Test Runtime Application

1. Verify Runtime starts automatically on boot
2. Check display output (should show generic start page if no project loaded)
3. Test loading a project (if applicable)
4. Monitor system resources:

```bash
# CPU usage
htop

# Memory usage
free -h

# GPU memory
cat /sys/kernel/debug/mali0/gpu_memory

# Temperature
cat /sys/class/thermal/thermal_zone0/temp
```

### Step 6: Network Testing (Modbus/OPC UA)

```bash
# Test Ethernet connectivity
ethtool eth0

# Test TCP/IP stack
netstat -tulpn

# If you have Modbus server:
# Use modpoll or similar tool to test connectivity

# If you have OPC UA server:
# Test OPC UA client functionality from Runtime
```

---

## Troubleshooting

### Problem: Packages Not Available in Buildroot menuconfig

**Symptoms:**
```
mesa3d, weston, ffmpeg, openssl, mbedtls, gdb, htop, etc. not found in menuconfig
```

**Explanation:**
Different Buildroot versions have different package availability. The instructions were written for Buildroot 2024.02.x, but package availability varies across versions.

**Solutions:**

1. **Check your Buildroot version:**
```bash
cd ~/accutrack-build/buildroot/buildroot
cat .config | grep BR2_VERSION
# Or
make --version
```

2. **Search for package availability:**
```bash
# Search for a package in buildroot
make list-defconfigs | grep <package-name>
# Or check package directory
ls package/ | grep <package-name>
```

3. **Use alternative packages:**
   - No mesa3d/weston? → Use proprietary Mali drivers (already in instructions via rockchip-mali package)
   - No openssl? → Check for libressl, or build openssl as custom package
   - No ffmpeg? → Use libmpeg2 (already enabled), or add ffmpeg manually
   - No development tools? → Use basic alternatives (ps, top, cat /proc)

4. **Upgrade Buildroot** (if many packages missing):
```bash
cd ~/accutrack-build/buildroot/buildroot
git fetch --all
git checkout 2024.11.x  # Use latest stable
make clean
# Reconfigure from scratch
```

5. **Add custom packages manually:**
See Appendix H: Adding Custom Packages (below) for how to add missing packages.

### Problem: Git Repository Clone Failures

**Symptoms:**
```
fatal: Remote branch stable-5.10-rock3 not found in upstream origin
fatal: destination path already exists and is not an empty directory
```

**Solution:**

1. **Clean up existing directories:**
```bash
cd ~/accutrack-build
rm -rf kernel u-boot rkbin
```

2. **Clone the correct repositories:**
```bash
# Stable RK35xx kernel (recommended)
git clone https://github.com/unifreq/linux-5.10.y-rk35xx.git kernel

# Mainline U-Boot with Rock 3B support
git clone https://github.com/u-boot/u-boot.git u-boot
cd u-boot
git checkout v2024.10
cd ..

# Rockchip binaries (bootloader components)
git clone https://github.com/rockchip-linux/rkbin.git rkbin
```

3. **Verify the device tree exists:**
```bash
cd kernel
ls -la arch/arm64/boot/dts/rockchip/rk3568-rock-3b.dts
# Should show the file exists
cd ..
```

4. **Check available U-Boot defconfigs:**
```bash
cd u-boot
ls -la configs/ | grep rock-3b
# Should show: rock-3b-rk3568_defconfig
cd ..
```

**Alternative if unifreq repo has issues:**
```bash
# Try Armbian's fork
git clone https://github.com/armbian/linux-rockchip.git -b rk-5.10-rkr6 kernel
```

### Problem: Boot Hangs at U-Boot

**Solution:**
1. Check U-Boot environment variables
2. Verify kernel and DTB are at correct offsets
3. Check bootargs in U-Boot

```bash
# Access U-Boot console (press any key during boot)
printenv
# Verify bootcmd and bootargs

# Manually boot kernel
load mmc 0:1 ${kernel_addr_r} /boot/Image
load mmc 0:1 ${fdt_addr_r} /boot/rk3568-rock-3b.dtb
booti ${kernel_addr_r} - ${fdt_addr_r}
```

### Problem: Kernel Panic on Boot

**Solution:**
1. Check kernel command line parameters
2. Verify rootfs partition is correct
3. Check filesystem integrity

```bash
# In U-Boot, check bootargs:
setenv bootargs "console=ttyS2,1500000 root=/dev/mmcblk0p1 rootwait rw earlycon"
boot

# If mounting rootfs fails, boot from recovery and run:
fsck.ext4 -f /dev/mmcblk0p1
```

### Problem: Runtime Service Fails to Start

**Solution:**
1. Check service status:
```bash
systemctl status runtime.service
journalctl -u runtime.service -b
```

2. Common issues:
   - Qt platform plugin not found: Check QT_QPA_PLATFORM
   - EGLFS initialization failed: Check GPU driver and /dev/dri/card0
   - Missing libraries: `ldd /usr/bin/Runtime`

3. Manual test:
```bash
# Stop service
systemctl stop runtime.service

# Run manually with debug
export QT_DEBUG_PLUGINS=1
export QT_LOGGING_RULES="*.debug=true"
/usr/bin/Runtime
```

### Problem: No Display Output (HDMI)

**Solution:**
1. Check HDMI connection
2. Verify DRM/KMS driver:
```bash
dmesg | grep -i drm
ls -l /dev/dri/
```

3. Check Qt KMS config:
```bash
cat /etc/qt-kms-config.json
# Try removing config to use auto-detection
```

4. Test with simple EGLFS app:
```bash
export QT_QPA_PLATFORM=eglfs
export QT_QPA_EGLFS_DEBUG=1
# Run any Qt application
```

### Problem: NPU Not Accessible

**Solution:**
1. Check NPU driver:
```bash
lsmod | grep galcore
dmesg | grep -i npu
```

2. Load driver manually:
```bash
modprobe galcore
```

3. Check device permissions:
```bash
ls -l /dev/rknpu
# Should be accessible by root or your user
```

4. Verify RKNN runtime:
```bash
ldconfig -p | grep rknn
# Should show librknnrt.so
```

### Problem: Network Not Working

**Solution:**
1. Check interface:
```bash
ip link show
ip addr show
```

2. Check systemd-networkd:
```bash
systemctl status systemd-networkd
journalctl -u systemd-networkd
```

3. Manual network setup:
```bash
ip link set eth0 up
dhclient eth0
# Or static:
ip addr add 192.168.1.100/24 dev eth0
ip route add default via 192.168.1.1
```

### Problem: Slow Boot Time

**Solution:**
1. Analyze boot time:
```bash
systemd-analyze
systemd-analyze blame
systemd-analyze critical-chain
```

2. Disable slow services:
```bash
systemctl disable <slow-service>
```

3. Reduce boot delay in U-Boot:
```bash
# In U-Boot console
setenv bootdelay 0
saveenv
```

### Problem: Out of Space

**Solution:**
1. Check disk usage:
```bash
df -h
du -sh /var/log/*
```

2. Clean logs:
```bash
journalctl --vacuum-time=1d
```

3. Resize partition (if needed):
```bash
# Extend partition to use full SD card
fdisk /dev/mmcblk0
# Delete and recreate partition with larger size
# Then resize filesystem
resize2fs /dev/mmcblk0p1
```

### Problem: Qt Application Crashes

**Solution:**
1. Get backtrace:
```bash
# Enable core dumps
ulimit -c unlimited
# Run application
./Runtime
# If crashes, analyze core dump
gdb /usr/bin/Runtime core
bt
```

2. Check for missing libraries:
```bash
ldd /usr/bin/Runtime
```

3. Verify Qt plugins:
```bash
export QT_DEBUG_PLUGINS=1
/usr/bin/Runtime
```

---

## Appendix A: Build Configuration Summary

### Recommended buildroot defconfig

Save this as: `~/accutrack-build/buildroot/buildroot/configs/accutrack_rock3b_defconfig`

```makefile
# Architecture
BR2_aarch64=y
BR2_cortex_a55=y

# Toolchain
BR2_TOOLCHAIN_BUILDROOT_GLIBC=y
BR2_GCC_VERSION_11_X=y
BR2_TOOLCHAIN_BUILDROOT_CXX=y
BR2_TOOLCHAIN_BUILDROOT_WCHAR=y
BR2_TOOLCHAIN_BUILDROOT_LOCALE=y

# System
BR2_TARGET_GENERIC_HOSTNAME="accutrack-hmi"
BR2_TARGET_GENERIC_ISSUE="Welcome to AccuTrackOS"
BR2_ROOTFS_DEVICE_CREATION_DYNAMIC_EUDEV=y
BR2_INIT_SYSTEMD=y
BR2_ROOTFS_OVERLAY="board/accutrack/rootfs-overlay"
BR2_ROOTFS_POST_BUILD_SCRIPT="board/accutrack/post-build.sh"
BR2_ROOTFS_POST_IMAGE_SCRIPT="board/accutrack/post-image.sh"

# Kernel
BR2_LINUX_KERNEL=y
BR2_LINUX_KERNEL_CUSTOM_GIT=y
BR2_LINUX_KERNEL_CUSTOM_REPO_URL="https://github.com/unifreq/linux-5.10.y-rk35xx.git"
BR2_LINUX_KERNEL_CUSTOM_REPO_VERSION="main"
BR2_LINUX_KERNEL_USE_CUSTOM_CONFIG=y
BR2_LINUX_KERNEL_CUSTOM_CONFIG_FILE="$(TOPDIR)/../kernel/defconfig_rock3b_accutrack"
BR2_LINUX_KERNEL_DTS_SUPPORT=y
BR2_LINUX_KERNEL_INTREE_DTS_NAME="rockchip/rk3568-rock-3b"

# Bootloader
BR2_TARGET_UBOOT=y
BR2_TARGET_UBOOT_BUILD_SYSTEM_KCONFIG=y
BR2_TARGET_UBOOT_CUSTOM_GIT=y
BR2_TARGET_UBOOT_CUSTOM_REPO_URL="https://github.com/u-boot/u-boot.git"
BR2_TARGET_UBOOT_CUSTOM_REPO_VERSION="v2024.10"
BR2_TARGET_UBOOT_BOARD_DEFCONFIG="rock-3b-rk3568"
BR2_TARGET_UBOOT_NEEDS_DTC=y
BR2_TARGET_UBOOT_NEEDS_OPENSSL=y
BR2_TARGET_UBOOT_NEEDS_PYLIBFDT=y
BR2_TARGET_UBOOT_NEEDS_ATF_BL31=y
BR2_TARGET_UBOOT_NEEDS_ATF_BL31_ELF=y
BR2_TARGET_UBOOT_SPL=y
BR2_TARGET_UBOOT_SPL_NAME="u-boot-spl.bin"

# Packages - Qt6
BR2_PACKAGE_QT6BASE=y
BR2_PACKAGE_QT6BASE_GUI=y
BR2_PACKAGE_QT6BASE_WIDGETS=y
BR2_PACKAGE_QT6BASE_OPENGL=y
BR2_PACKAGE_QT6BASE_OPENGL_ES2=y
BR2_PACKAGE_QT6BASE_EGLFS=y
BR2_PACKAGE_QT6BASE_LINUXFB=y
BR2_PACKAGE_QT6BASE_FONTCONFIG=y
BR2_PACKAGE_QT6BASE_PNG=y
BR2_PACKAGE_QT6BASE_JPEG=y
BR2_PACKAGE_QT6BASE_SQL=y
BR2_PACKAGE_QT6BASE_SQL_SQLITE=y
BR2_PACKAGE_QT6BASE_NETWORK=y
BR2_PACKAGE_QT6DECLARATIVE=y
BR2_PACKAGE_QT6SERIALPORT=y
BR2_PACKAGE_QT6SVG=y

# Packages - Graphics (limited availability)
BR2_PACKAGE_LIBDRM=y
# Note: mesa3d, wayland, weston may not be available
# Will use proprietary Mali drivers via custom package

# Packages - Multimedia
BR2_PACKAGE_LIBMPEG2=y

# Packages - Networking
BR2_PACKAGE_LIBCURL=y
BR2_PACKAGE_OPEN62541=y  # CRITICAL: OPC UA support for SCADA
BR2_PACKAGE_LIBMODBUS=y  # CRITICAL: Modbus TCP/RTU support for SCADA
# Note: openssl, mbedtls may not be available in this version

# Packages - Networking Applications
BR2_PACKAGE_DROPBEAR=y  # Or BR2_PACKAGE_OPENSSH=y
BR2_PACKAGE_ETHTOOL=y

# Packages - Custom
BR2_PACKAGE_RKNPU2=y
BR2_PACKAGE_ROCKCHIP_MALI=y
BR2_PACKAGE_ACCUTRACK_RUNTIME=y

# Filesystem
BR2_TARGET_ROOTFS_EXT2=y
BR2_TARGET_ROOTFS_EXT2_4=y
BR2_TARGET_ROOTFS_TAR=y
```

Load this config:
```bash
cd ~/accutrack-build/buildroot/buildroot
make accutrack_rock3b_defconfig
```

---

## Appendix B: Directory Structure Overview

```
~/accutrack-build/
├── buildroot/
│   └── buildroot/
│       ├── board/
│       │   └── accutrack/
│       │       ├── genimage-rock3b.cfg
│       │       ├── post-build.sh
│       │       ├── post-image.sh
│       │       ├── uboot-env.txt
│       │       └── rootfs-overlay/
│       │           ├── boot/
│       │           │   └── extlinux/
│       │           │       └── extlinux.conf
│       │           ├── etc/
│       │           │   ├── hostname
│       │           │   ├── qt-kms-config.json
│       │           │   ├── profile.d/
│       │           │   │   └── qt6-env.sh
│       │           │   └── systemd/
│       │           │       ├── network/
│       │           │       │   └── 10-eth0.network
│       │           │       ├── system/
│       │           │       │   ├── rknpu-init.service
│       │           │       │   └── runtime.service
│       │           │       └── system-preset/
│       │           │           └── 90-accutrack.preset
│       │           └── opt/
│       │               └── accutrack/
│       ├── configs/
│       │   └── accutrack_rock3b_defconfig
│       ├── output/
│       │   └── images/
│       │       ├── Image
│       │       ├── rk3568-rock-3b.dtb
│       │       ├── rootfs.ext4
│       │       ├── u-boot.itb
│       │       ├── idbloader.img
│       │       └── boot.img
│       └── package/
│           ├── rknpu2/
│           │   ├── Config.in
│           │   └── rknpu2.mk
│           ├── rockchip-mali/
│           │   ├── Config.in
│           │   └── rockchip-mali.mk
│           └── accutrack-runtime/
│               ├── Config.in
│               └── accutrack-runtime.mk
├── kernel/
│   ├── arch/arm64/boot/dts/rockchip/
│   │   └── rk3568-rock-3b.dts
│   └── defconfig_rock3b_accutrack
├── u-boot/
├── rkbin/
├── runtime/
│   ├── Runtime (executable)
│   ├── lib/
│   ├── qml/
│   ├── resources/
│   └── default-project/
└── output/
```

---

## Appendix C: Quick Reference Commands

### Minimal Required Packages (Based on Availability)

**When configuring buildroot menuconfig, focus on these ESSENTIAL packages:**

#### Target Packages → Libraries → Graphics
```
[*] libdrm
[ ] wayland (if available, but not required)
```

#### Target Packages → Libraries → Multimedia  
```
[*] libmpeg2 (for video if needed)
```

#### Target Packages → Libraries → Networking
```
[*] libcurl (if available)
[*] open62541    ← CRITICAL for OPC UA
[*] libmodbus    ← CRITICAL for Modbus
```

#### Target Packages → Networking Applications
```
[*] dropbear (for SSH) OR openssh
[*] ethtool
[ ] tcpdump (if available)
```

#### Target Packages → Graphic libraries and applications
```
[*] qt6base
    [*] gui module
    [*] widgets module  
    [*] opengl support → OpenGL ES 2.0+
    [*] eglfs support
        [*] eglfs kms support
    [*] linuxfb support
    [*] fontconfig support
    [*] PNG support
    [*] JPEG support
    [*] enable network module
    [*] enable sql module → SQLite support

[*] qt6declarative (QML)
[*] qt6serialport (for Modbus RTU if needed)
[*] qt6svg
[*] qt6imageformats
```

#### Custom Packages (you'll add these)
```
[*] rknpu2           ← For NPU support
[*] rockchip-mali    ← For GPU support
[*] accutrack-runtime ← Your Runtime application
```

### Build Commands
```bash
# Full clean build
cd ~/accutrack-build/buildroot/buildroot
make clean
make all -j$(nproc)

# Rebuild specific package
make <package>-rebuild

# Rebuild Runtime only
make accutrack-runtime-rebuild

# Reconfigure buildroot
make menuconfig

# Reconfigure kernel
make linux-menuconfig

# Reconfigure U-Boot
make uboot-menuconfig

# Save buildroot config
make savedefconfig BR2_DEFCONFIG=configs/accutrack_rock3b_defconfig
```

### Testing Commands
```bash
# On target board (via SSH)
systemctl status runtime.service
journalctl -u runtime.service -f
dmesg | grep -i mali
dmesg | grep -i npu
systemd-analyze blame
```

### Update Commands
```bash
# Update kernel
cd ~/accutrack-build/kernel
git pull
cd ~/accutrack-build/buildroot/buildroot
make linux-rebuild

# Update Runtime application
# (Copy new binary to ~/accutrack-build/runtime/)
make accutrack-runtime-rebuild

# Rebuild image
make all
```

---

## Appendix D: Performance Tuning

### CPU Governor
```bash
# Set performance governor
echo performance > /sys/devices/system/cpu/cpu0/cpufreq/scaling_governor
echo performance > /sys/devices/system/cpu/cpu1/cpufreq/scaling_governor
echo performance > /sys/devices/system/cpu/cpu2/cpufreq/scaling_governor
echo performance > /sys/devices/system/cpu/cpu3/cpufreq/scaling_governor
```

### GPU Frequency
```bash
# Check current GPU frequency
cat /sys/class/devfreq/fde60000.gpu/cur_freq

# Set to performance mode
echo performance > /sys/class/devfreq/fde60000.gpu/governor
```

### NPU Optimization
```bash
# NPU frequency settings
# (Device path may vary, check dmesg)
echo 1000000000 > /sys/devices/platform/fde40000.npu/devfreq/fde40000.npu/max_freq
```

---

## Appendix E: Backup and Recovery

### Create Backup
```bash
# Backup SD card/eMMC
sudo dd if=/dev/sdX of=~/accutrack-backup-$(date +%Y%m%d).img bs=4M status=progress

# Compress backup
gzip ~/accutrack-backup-$(date +%Y%m%d).img
```

### Restore Backup
```bash
# Restore from backup
gunzip -c ~/accutrack-backup-20250127.img.gz | sudo dd of=/dev/sdX bs=4M status=progress
```

---

## Appendix F: Development Workflow

### Iterative Development
```bash
# 1. Make changes to Runtime application
# 2. Cross-compile for ARM64
# 3. Copy new binary to runtime directory
cp /path/to/new/Runtime ~/accutrack-build/runtime/

# 4. Rebuild just the Runtime package
cd ~/accutrack-build/buildroot/buildroot
make accutrack-runtime-rebuild

# 5. Rebuild filesystem image
make all

# 6. Flash to SD card for testing
sudo dd if=output/images/boot.img of=/dev/sdX bs=4M status=progress conv=fsync

# 7. Test on board
# 8. Iterate
```

### Remote Debugging
```bash
# On development machine
aarch64-linux-gnu-gdb

# Connect to gdbserver on target
target remote <board-ip>:1234

# Debug Runtime
file ~/accutrack-build/runtime/Runtime
continue
```

---

## Appendix H: Adding Custom Packages to Buildroot

If essential packages are missing from your Buildroot version, you can add them manually.

### Example: Adding OpenSSL (if not available)

1. **Create package directory:**
```bash
cd ~/accutrack-build/buildroot/buildroot
mkdir -p package/openssl
```

2. **Create Config.in:**
```bash
cat > package/openssl/Config.in << 'EOF'
config BR2_PACKAGE_OPENSSL
    bool "openssl"
    help
      OpenSSL is an open-source implementation of the SSL and TLS protocols.
      
      https://www.openssl.org
EOF
```

3. **Create package makefile:**
```bash
cat > package/openssl/openssl.mk << 'EOF'
################################################################################
#
# openssl
#
################################################################################

OPENSSL_VERSION = 3.0.13
OPENSSL_SITE = https://www.openssl.org/source
OPENSSL_SOURCE = openssl-$(OPENSSL_VERSION).tar.gz
OPENSSL_LICENSE = Apache-2.0
OPENSSL_INSTALL_STAGING = YES

OPENSSL_CONF_OPTS = \
    --prefix=/usr \
    --openssldir=/etc/ssl \
    no-tests \
    shared \
    $(if $(BR2_STATIC_LIBS),no-shared,shared) \
    $(if $(BR2_aarch64),linux-aarch64,linux-generic64)

define OPENSSL_CONFIGURE_CMDS
    (cd $(@D); \
        $(TARGET_CONFIGURE_OPTS) \
        ./Configure $(OPENSSL_CONF_OPTS) \
    )
endef

define OPENSSL_BUILD_CMDS
    $(TARGET_MAKE_ENV) $(MAKE) -C $(@D)
endef

define OPENSSL_INSTALL_STAGING_CMDS
    $(TARGET_MAKE_ENV) $(MAKE) -C $(@D) DESTDIR=$(STAGING_DIR) install
endef

define OPENSSL_INSTALL_TARGET_CMDS
    $(TARGET_MAKE_ENV) $(MAKE) -C $(@D) DESTDIR=$(TARGET_DIR) install
endef

$(eval $(generic-package))
EOF
```

4. **Register in main Config.in:**
```bash
# Edit package/Config.in
# Add under "Networking applications" or appropriate section:
    source "package/openssl/Config.in"
```

5. **Enable in menuconfig:**
```bash
make menuconfig
# Navigate to package location and enable
```

### Example: Adding mbedTLS

Similar process:
```bash
mkdir -p package/mbedtls
```

Create mbedtls.mk referencing https://github.com/Mbed-TLS/mbedtls releases.

### Pre-built Package Alternative

If building from source is problematic:

1. **Download pre-built ARM64 package** (e.g., from Debian/Ubuntu ARM64 repos)
2. **Extract and add to rootfs-overlay:**
```bash
mkdir -p board/accutrack/rootfs-overlay/usr/lib
cp libssl.so* board/accutrack/rootfs-overlay/usr/lib/
```

### Buildroot External Tree Method

For multiple custom packages, use BR2_EXTERNAL:

```bash
cd ~/accutrack-build
mkdir buildroot-external
cd buildroot-external

# Create structure
mkdir -p {package,board,configs}
cat > external.desc << 'EOF'
name: ACCUTRACK
desc: AccuTrackOS custom packages
EOF

cat > external.mk << 'EOF'
include $(sort $(wildcard $(BR2_EXTERNAL_ACCUTRACK_PATH)/package/*/*.mk))
EOF

cat > Config.in << 'EOF'
source "$BR2_EXTERNAL_ACCUTRACK_PATH/package/Config.in"
EOF
```

Then build with:
```bash
cd ~/accutrack-build/buildroot/buildroot
make BR2_EXTERNAL=../../buildroot-external menuconfig
```

---

## Support and Resources

### Official Documentation
- **Radxa Rock 3B**: https://docs.radxa.com/en/rock3/rock3b
- **Buildroot Manual**: https://buildroot.org/downloads/manual/manual.html
- **Qt 6 Documentation**: https://doc.qt.io/qt-6/
- **Rockchip Linux**: https://github.com/rockchip-linux

### Community
- **Radxa Forum**: https://forum.radxa.com/
- **Buildroot Mailing List**: https://buildroot.org/lists.html

### Troubleshooting
For issues specific to AccuTrackOS, review:
1. Build logs: `~/accutrack-build/buildroot/buildroot/output/build/build-time.log`
2. Package logs: `~/accutrack-build/buildroot/buildroot/output/build/<package>/`
3. System journal: `journalctl -b` on target

---

## Conclusion

You now have comprehensive instructions to build AccuTrackOS for your Radxa Rock 3B HMI system. The build process will create a minimal, optimized Linux system with:

- Qt 6.x Runtime environment
- Mali-G52 GPU support with OpenGL ES
- RK3568J NPU support for ML inference
- Ethernet networking for OPC UA and Modbus TCP/IP
- Fast boot optimized for HMI applications
- SSH access for development and debugging

Expected build time: **1-3 hours** (first build)  
Expected boot time: **10-20 seconds** (to Runtime display)

Good luck with your AccuTrackOS build!