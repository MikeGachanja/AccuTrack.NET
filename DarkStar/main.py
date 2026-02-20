"""
Robot Sensor Data OPC UA Server
Exposes robot sensor data via OPC UA server for remote monitoring and control.
"""

from opcua import ua, Server
import time
import logging
from typing import Callable, Optional

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


class RobotSensorOPCServer:
    """
    OPC UA Server for exposing robot sensor data.
    Exposes 5 sensor tags that can be subscribed to by OPC UA clients.
    """
    
    def __init__(
        self,
        endpoint: str = "opc.tcp://0.0.0.0:4840/robot/",
        server_name: str = "Robot OPC UA Server",
        namespace_uri: str = "http://yourcompany/robot",
        update_interval: float = 0.1
    ):
        """
        Initialize the OPC UA server.
        
        Args:
            endpoint: OPC UA server endpoint URL
            server_name: Name of the OPC UA server
            namespace_uri: Namespace URI for the robot data
            update_interval: Update interval in seconds for sensor readings
        """
        self.server = Server()
        self.server.set_endpoint(endpoint)
        self.server.set_server_name(server_name)
        self.update_interval = update_interval
        
        # Register namespace
        self.idx = self.server.register_namespace(namespace_uri)
        
        # Create object structure
        self.objects = self.server.get_objects_node()
        self.robot = self.objects.add_object(self.idx, "Robot")
        
        # Initialize sensor variables (will be created in setup_sensors)
        self.sensors = {}
        
        # Sensor reading callbacks (can be overridden)
        self.sensor_readers = {}
        
        logger.info(f"OPC UA Server initialized: {server_name}")
        logger.info(f"Endpoint: {endpoint}")
    
    def setup_sensors(self):
        """
        Set up the 5 sensor tags in the OPC UA server.
        """
        # Sensor 1: Joint Position (writable for control)
        self.sensors['joint_position'] = self.robot.add_variable(
            self.idx, 
            "Joint1_Position", 
            0.0,
            varianttype=ua.VariantType.Float
        )
        self.sensors['joint_position'].set_writable()
        self.sensors['joint_position'].set_display_name("Joint 1 Position")
        self.sensors['joint_position'].set_description("Current position of Joint 1 in degrees")
        
        # Sensor 2: Motor Temperature
        self.sensors['motor_temperature'] = self.robot.add_variable(
            self.idx,
            "Motor_Temperature",
            25.0,
            varianttype=ua.VariantType.Float
        )
        self.sensors['motor_temperature'].set_display_name("Motor Temperature")
        self.sensors['motor_temperature'].set_description("Motor temperature in Celsius")
        
        # Sensor 3: Velocity
        self.sensors['velocity'] = self.robot.add_variable(
            self.idx,
            "Velocity",
            0.0,
            varianttype=ua.VariantType.Float
        )
        self.sensors['velocity'].set_display_name("Robot Velocity")
        self.sensors['velocity'].set_description("Current velocity in m/s")
        
        # Sensor 4: Force/Torque
        self.sensors['force'] = self.robot.add_variable(
            self.idx,
            "Force",
            0.0,
            varianttype=ua.VariantType.Float
        )
        self.sensors['force'].set_display_name("Force Sensor")
        self.sensors['force'].set_description("Force reading in Newtons")
        
        # Sensor 5: Status (Boolean)
        self.sensors['status'] = self.robot.add_variable(
            self.idx,
            "Robot_Status",
            True,
            varianttype=ua.VariantType.Boolean
        )
        self.sensors['status'].set_display_name("Robot Status")
        self.sensors['status'].set_description("Robot operational status (True=Ready, False=Error)")
        
        logger.info("5 sensor tags configured:")
        logger.info("  - Joint1_Position (Float, writable)")
        logger.info("  - Motor_Temperature (Float)")
        logger.info("  - Velocity (Float)")
        logger.info("  - Force (Float)")
        logger.info("  - Robot_Status (Boolean)")
    
    def register_sensor_reader(self, sensor_name: str, reader_func: Callable[[], any]):
        """
        Register a custom function to read sensor data.
        
        Args:
            sensor_name: Name of the sensor (e.g., 'joint_position', 'motor_temperature')
            reader_func: Function that returns the sensor value
        """
        if sensor_name in self.sensors:
            self.sensor_readers[sensor_name] = reader_func
            logger.info(f"Registered custom reader for sensor: {sensor_name}")
        else:
            logger.warning(f"Unknown sensor name: {sensor_name}")
    
    def read_joint_position(self) -> float:
        """
        Read joint position from robot hardware.
        Override this method or register a custom reader.
        
        Returns:
            Joint position in degrees
        """
        if 'joint_position' in self.sensor_readers:
            return self.sensor_readers['joint_position']()
        # Placeholder: Replace with actual hardware read
        return 0.0
    
    def read_motor_temperature(self) -> float:
        """
        Read motor temperature from robot hardware.
        Override this method or register a custom reader.
        
        Returns:
            Temperature in Celsius
        """
        if 'motor_temperature' in self.sensor_readers:
            return self.sensor_readers['motor_temperature']()
        # Placeholder: Replace with actual hardware read
        return 25.0
    
    def read_velocity(self) -> float:
        """
        Read velocity from robot hardware.
        Override this method or register a custom reader.
        
        Returns:
            Velocity in m/s
        """
        if 'velocity' in self.sensor_readers:
            return self.sensor_readers['velocity']()
        # Placeholder: Replace with actual hardware read
        return 0.0
    
    def read_force(self) -> float:
        """
        Read force/torque from robot hardware.
        Override this method or register a custom reader.
        
        Returns:
            Force in Newtons
        """
        if 'force' in self.sensor_readers:
            return self.sensor_readers['force']()
        # Placeholder: Replace with actual hardware read
        return 0.0
    
    def read_status(self) -> bool:
        """
        Read robot status from hardware.
        Override this method or register a custom reader.
        
        Returns:
            True if robot is ready, False if error
        """
        if 'status' in self.sensor_readers:
            return self.sensor_readers['status']()
        # Placeholder: Replace with actual hardware read
        return True
    
    def update_sensor_values(self):
        """
        Update all sensor values from hardware readings.
        """
        try:
            self.sensors['joint_position'].set_value(self.read_joint_position())
            self.sensors['motor_temperature'].set_value(self.read_motor_temperature())
            self.sensors['velocity'].set_value(self.read_velocity())
            self.sensors['force'].set_value(self.read_force())
            self.sensors['status'].set_value(self.read_status())
        except Exception as e:
            logger.error(f"Error updating sensor values: {e}", exc_info=True)
    
    def start(self):
        """
        Start the OPC UA server.
        """
        try:
            self.server.start()
            logger.info("OPC UA Server started successfully")
        except Exception as e:
            logger.error(f"Failed to start OPC UA server: {e}", exc_info=True)
            raise
    
    def stop(self):
        """
        Stop the OPC UA server.
        """
        try:
            self.server.stop()
            logger.info("OPC UA Server stopped")
        except Exception as e:
            logger.error(f"Error stopping OPC UA server: {e}", exc_info=True)
    
    def run(self):
        """
        Main run loop - starts server and continuously updates sensor values.
        """
        self.setup_sensors()
        self.start()
        
        try:
            logger.info("Server running. Press Ctrl+C to stop.")
            while True:
                self.update_sensor_values()
                time.sleep(self.update_interval)
        except KeyboardInterrupt:
            logger.info("Received shutdown signal")
        finally:
            self.stop()


def main():
    """
    Main entry point for the robot sensor OPC UA server.
    """
    # Create and configure the server
    server = RobotSensorOPCServer(
        endpoint="opc.tcp://0.0.0.0:4840/robot/",
        server_name="Robot OPC UA Server",
        namespace_uri="http://yourcompany/robot",
        update_interval=0.1  # Update every 100ms
    )
    
    # Example: Register custom sensor readers if you have hardware interfaces
    # server.register_sensor_reader('joint_position', your_hardware.read_joint)
    # server.register_sensor_reader('motor_temperature', your_hardware.read_temp)
    # etc.
    
    # Run the server
    server.run()


if __name__ == "__main__":
    main()
