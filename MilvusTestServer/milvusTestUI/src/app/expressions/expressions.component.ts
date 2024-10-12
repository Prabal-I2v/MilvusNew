import {Component, ViewEncapsulation} from '@angular/core';

@Component({
  selector: 'app-expressions',
  templateUrl: './expressions.component.html',
  styleUrl: './expressions.component.scss',
  encapsulation : ViewEncapsulation.Emulated
})
export class ExpressionsComponent {

  selectedExpressionType: string = '';
  expression: string = '';

  // For comparison expression
  leftOperand: string = '';
  leftOperandType: string = '';
  comparisonOperator: string = '';
  rightOperand: string = '';
  rightOperandType: string = '';

  // For logical expression
  logicalExpr1: string = '';
  logicalOperator: string = '';
  logicalExpr2: string = '';

  // For arithmetic expression
  arithmeticOperand1: string = '';
  arithmeticOperator: string = '';
  arithmeticOperand2: string = '';

  // For term expression
  termField: string = '';
  termValues: string = '';

  // For match expression
  matchField: string = '';
  matchValue: string = '';

  // For JSON Array expression
  jsonArrayField: string = '';
  jsonArrayIndex: string = '';

  // For array expression
  arrayField: string = '';
  arrayIndex: string = '';

  onExpressionTypeChange() {
    this.expression = ''; // Reset expression on type change
  }

  buildExpression() {
    switch (this.selectedExpressionType) {
      case 'comparison':
        const leftValue = this.formatValue(this.leftOperand, this.leftOperandType);
        const rightValue = this.formatValue(this.rightOperand, this.rightOperandType);
        this.expression = `${leftValue} ${this.comparisonOperator} ${rightValue}`;
        break;
      case 'logical':
        this.expression = `(${this.logicalExpr1} ${this.logicalOperator} ${this.logicalExpr2})`;
        break;
      case 'arithmetic':
        this.expression = `(${this.arithmeticOperand1} ${this.arithmeticOperator} ${this.arithmeticOperand2})`;
        break;
      case 'term':
        const values = this.termValues.split(',').map(val => val.trim()).join(', ');
        this.expression = `${this.termField} in [${values}]`;
        break;
      case 'match':
        this.expression = `${this.matchField} = ${this.matchValue}`;
        break;
      case 'jsonArray':
        this.expression = `${this.jsonArrayField}[${this.jsonArrayIndex}]`;
        break;
      case 'array':
        this.expression = `${this.arrayField}[${this.arrayIndex}]`;
        break;
      default:
        this.expression = '';
    }
  }

  formatValue(value: string, type: string): string {
    switch (type) {
      case 'string':
        return `'${value}'`;
      case 'number':
        return value; // Assuming it's a number already
      case 'boolean':
        return value.toLowerCase(); // Assuming input like 'true' or 'false'
      default:
        return value; // Default to just the value if no type is selected
    }
  }
}
