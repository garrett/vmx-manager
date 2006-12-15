
ASSEMBLY = vmx-manager.exe

asmdir = $(pkglibdir)
asm_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(srcdir)/$(ASSEMBLY).config

bin_SCRIPTS = vmx-manager

VMXMAN_SOURCE =				\
	CdConfigDialog.cs		\
	CdDriveCombo.cs			\
	ConfigDialog.cs			\
	DeviceModel.cs			\
	DeviceView.cs			\
	DiskProgressPane.cs		\
	EntryPoint.cs			\
	Enums.cs			\
	EthernetConfigDialog.cs		\
	GuestOperatingSystem.cs		\
	HardDiskConfigDialog.cs		\
	HigMessageDialog.cs		\
	IconThemeUtils.cs		\
	LinkLabel.cs			\
	MainController.cs		\
	MainWindow.cs			\
	MessagePane.cs			\
	OSModel.cs			\
	Utility.cs			\
	VirtualCdDrive.cs		\
	VirtualDisk.cs			\
	VirtualEthernet.cs		\
	VirtualHardDisk.cs		\
	VirtualMachine.cs		\
	VirtualMachineManager.cs	\
	VMModel.cs			\
	VMView.cs

VMXMAN_RESOURCES =		\
	defaults.vmx		\
	operating-systems.csv	\
	vmx-manager.glade	\
	vmx-manager.xml		\
	vmx-manager-config.xml	\
	gnome-dev-cdrom.png     \
	gnome-dev-harddisk.png  \
	gnome-dev-ethernet.png  \
	gnome-dev-floppy.png

VMXMAN_EXPANDED_SOURCE = $(foreach source,$(VMXMAN_SOURCE), $(srcdir)/$(source))

VMXMAN_EXPANDED_RESOURCES = $(foreach resource,$(VMXMAN_RESOURCES), $(top_srcdir)/data/$(resource))

VMXMAN_EXPANDED_RESOURCES_BUILD = $(foreach resource,$(VMXMAN_EXPANDED_RESOURCES), $(addprefix -resource:,$(resource)),$(notdir $(resource)))

vmx-manager.exe: $(VMXMAN_SOURCE) $(VMXMAN_EXPANDED_RESOURCES)
	$(MCS) -debug -out:$@ $(VMXMAN_EXPANDED_SOURCE) $(VMXMAN_EXPANDED_RESOURCES_BUILD) $(VMXMAN_LIBS) -r:$(top_builddir)/hal-sharp/Hal.dll

CLEANFILES = $(bin_SCRIPTS) $(ASSEMBLY)
EXTRA_DIST = $(VMXMAN_SOURCE) vmx-manager.in vmx-manager.exe.config
DISTCLEANFILES = *.mdb

