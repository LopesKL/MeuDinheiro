import { createGlobalStyle } from 'styled-components';
import { colors } from './colors';

const GlobalStyle = createGlobalStyle`
  * {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
  }

  html, body {
    height: 100%;
    font-family: 'Montserrat', sans-serif;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
  }

  #root {
    height: 100%;
  }

  /* Scrollbar customization */
  ::-webkit-scrollbar {
    width: 8px;
    height: 8px;
  }

  ::-webkit-scrollbar-track {
    background: ${colors.background};
  }

  ::-webkit-scrollbar-thumb {
    background: ${colors.text.secondary};
    border-radius: 4px;
  }

  ::-webkit-scrollbar-thumb:hover {
    background: ${colors.text.primary};
  }

  /* Ant Design Form.Item margin */
  .ant-form-item {
    margin-bottom: 10px;
  }

  /* Ant Design transitions optimization */
  .ant-btn,
  .ant-input,
  .ant-select-selector,
  .ant-picker {
    transition: all 0.2s cubic-bezier(0.645, 0.045, 0.355, 1) !important;
  }

  /* Hover effects */
  .ant-card:hover {
    transform: translateY(-1px);
    transition: transform 0.2s cubic-bezier(0.645, 0.045, 0.355, 1);
  }

  /* Performance optimizations */
  .ant-card,
  .ant-btn,
  .ant-input {
    will-change: transform;
    transform: translateZ(0);
  }

  /* Masked input styles */
  .ant-input-mask {
    width: 100%;
  }
`;

export default GlobalStyle;
