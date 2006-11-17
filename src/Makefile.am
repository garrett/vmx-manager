
ASSEMBLIES = vmx-manager.exe

asmdir = $(pkglibdir)
asm_DATA = $(ASSEMBLIES)

bin_SCRIPTS = vmx-manager

VMXMAN_SOURCE =				\
	ConfigDialog.cs			\
	EntryPoint.cs			\
	MainController.cs		\
	MainWindow.cs			\
	OSModel.cs			\
	Utility.cs			\
	VirtualMachine.cs		\
	VirtualMachineManager.cs	\
	VMModel.cs			\
	VMView.cs

VMXMAN_RESOURCES = defaults.vmx operating-systems.csv vmx-manager.glade vmx-manager.xml

VMXMAN_EXPANDED_RESOURCES = $(foreach resource,$(VMXMAN_RESOURCES), $(top_srcdir)/data/$(resource))

VMXMAN_EXPANDED_RESOURCES_BUILD = $(foreach resource,$(VMXMAN_EXPANDED_RESOURCES), $(addprefix -resource:,$(resource)),$(notdir $(resource)))

vmx-manager.exe: $(VMXMAN_SOURCE) $(VMXMAN_EXPANDED_RESOURCES)
	$(MCS) -out:$@ $(VMXMAN_SOURCE) $(VMXMAN_EXPANDED_RESOURCES_BUILD) $(VMXMAN_LIBS)

CLEANFILES = $(bin_SCRIPTS) $(ASSEMBLIES)
