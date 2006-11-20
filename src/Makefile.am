
ASSEMBLY = vmx-manager.exe

asmdir = $(pkglibdir)
asm_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

bin_SCRIPTS = vmx-manager

VMXMAN_SOURCE =				\
	ConfigDialog.cs			\
	DeviceModel.cs			\
	DeviceView.cs			\
	EntryPoint.cs			\
	Enums.cs			\
	GuestOperatingSystem.cs		\
	IconThemeUtils.cs		\
	MainController.cs		\
	MainWindow.cs			\
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
	vmx-manager-config.xml

VMXMAN_EXPANDED_RESOURCES = $(foreach resource,$(VMXMAN_RESOURCES), $(top_srcdir)/data/$(resource))

VMXMAN_EXPANDED_RESOURCES_BUILD = $(foreach resource,$(VMXMAN_EXPANDED_RESOURCES), $(addprefix -resource:,$(resource)),$(notdir $(resource)))

vmx-manager.exe: $(VMXMAN_SOURCE) $(VMXMAN_EXPANDED_RESOURCES)
	$(MCS) -debug -out:$@ $(VMXMAN_SOURCE) $(VMXMAN_EXPANDED_RESOURCES_BUILD) $(VMXMAN_LIBS)

CLEANFILES = $(bin_SCRIPTS) $(ASSEMBLIES)
